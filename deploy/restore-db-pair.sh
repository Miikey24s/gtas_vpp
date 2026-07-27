#!/usr/bin/env bash
set -Eeuo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
COMPOSE_FILE="${COMPOSE_FILE:-docker-compose.prod.yml}"
DB_CONTAINER="${DB_CONTAINER:-gtas-vpp-db}"
PRIMARY_DB_NAME="${PRIMARY_DB_NAME:-GTAS_VPP_LIVE}"
IDENTITY_DB_NAME="${IDENTITY_DB_NAME:-GTAS_MENU}"
BACKUP_LABEL="${1:-}"
BACKUP_TIMESTAMP="${2:-}"
BACKUP_DIR="${3:-/var/opt/mssql/backup}"

if [[ "${RESTORE_PAIR_CONFIRM:-}" != "${PRIMARY_DB_NAME}+${IDENTITY_DB_NAME}" ]]; then
  echo "Refusing paired restore. Set RESTORE_PAIR_CONFIRM=${PRIMARY_DB_NAME}+${IDENTITY_DB_NAME} for this command only." >&2
  exit 2
fi

for database_name in "$PRIMARY_DB_NAME" "$IDENTITY_DB_NAME"; do
  if [[ ! "$database_name" =~ ^[A-Za-z0-9_]+$ ]]; then
    echo "Unsafe database name: $database_name" >&2
    exit 2
  fi
done

if [[ "$PRIMARY_DB_NAME" == "$IDENTITY_DB_NAME" ]]; then
  echo "The primary and identity database names must be different." >&2
  exit 2
fi

if [[ ! "$BACKUP_LABEL" =~ ^[A-Za-z0-9_-]+$ ]]; then
  echo "Backup label may contain only letters, numbers, underscores, and hyphens." >&2
  exit 2
fi

if [[ ! "$BACKUP_TIMESTAMP" =~ ^[0-9]{8}T[0-9]{6}Z$ ]]; then
  echo "Backup timestamp must use UTC YYYYMMDDTHHMMSSZ format." >&2
  exit 2
fi

if [[ "$BACKUP_DIR" != "/var/opt/mssql/backup" ]]; then
  echo "Paired restore accepts only the approved backup directory." >&2
  exit 2
fi

if [[ ! -f deploy-state.env || ! -f "$COMPOSE_FILE" || ! -f .env ]]; then
  echo "Run paired restore from a deployed release with state, Compose, and environment files." >&2
  exit 2
fi

BE_IMAGE="$(grep '^BE_IMAGE=' deploy-state.env | tail -n 1 | cut -d= -f2-)"
FE_IMAGE="$(grep '^FE_IMAGE=' deploy-state.env | tail -n 1 | cut -d= -f2-)"
if [[ -z "$BE_IMAGE" || -z "$FE_IMAGE" ]]; then
  echo "Current application image metadata is incomplete." >&2
  exit 2
fi
export BE_IMAGE FE_IMAGE

compose() {
  docker compose -f "$COMPOSE_FILE" "$@"
}

primary_source="${BACKUP_DIR}/${PRIMARY_DB_NAME}_${BACKUP_LABEL}_${BACKUP_TIMESTAMP}.bak"
identity_source="${BACKUP_DIR}/${IDENTITY_DB_NAME}_${BACKUP_LABEL}_${BACKUP_TIMESTAMP}.bak"

verify_backup() {
  local database_name="$1"
  local backup_file="$2"

  docker exec "$DB_CONTAINER" test -f "$backup_file"
  docker exec \
    -e BACKUP_FILE="$backup_file" \
    -e DB_NAME="$database_name" \
    "$DB_CONTAINER" bash -euc '
      /opt/mssql-tools18/bin/sqlcmd \
        -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -b \
        -Q "IF DB_ID(N'''$DB_NAME''') IS NULL THROW 51000, '''Expected restore target is missing.''', 1; RESTORE VERIFYONLY FROM DISK = N'''$BACKUP_FILE''' WITH CHECKSUM;"
    '
}

restore_database() {
  local database_name="$1"
  local backup_file="$2"

  docker exec \
    -e BACKUP_FILE="$backup_file" \
    -e DB_NAME="$database_name" \
    "$DB_CONTAINER" bash -euc '
      /opt/mssql-tools18/bin/sqlcmd \
        -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -b \
        -Q "ALTER DATABASE [$DB_NAME] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; RESTORE DATABASE [$DB_NAME] FROM DISK = N'''$BACKUP_FILE''' WITH REPLACE, RECOVERY; ALTER DATABASE [$DB_NAME] SET MULTI_USER;"
    '
}

ensure_multi_user() {
  local database_name="$1"
  docker exec -e DB_NAME="$database_name" "$DB_CONTAINER" bash -euc '
    /opt/mssql-tools18/bin/sqlcmd \
      -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -b \
      -Q "IF DB_ID(N'''$DB_NAME''') IS NOT NULL ALTER DATABASE [$DB_NAME] SET MULTI_USER;"
  '
}

start_apps() {
  compose up -d --wait --wait-timeout 180 backend frontend
}

verify_backup "$PRIMARY_DB_NAME" "$primary_source"
verify_backup "$IDENTITY_DB_NAME" "$identity_source"

apps_stopped=false
restore_started=false
recovery_timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
primary_recovery="${BACKUP_DIR}/${PRIMARY_DB_NAME}_before-pair-restore_${recovery_timestamp}.bak"
identity_recovery="${BACKUP_DIR}/${IDENTITY_DB_NAME}_before-pair-restore_${recovery_timestamp}.bak"

recover_on_exit() {
  local exit_code="$?"
  trap - EXIT
  set +e

  ensure_multi_user "$PRIMARY_DB_NAME" || true
  ensure_multi_user "$IDENTITY_DB_NAME" || true

  if [[ "$restore_started" == "true" ]]; then
    echo "Paired restore failed; restoring both databases from the verified pre-restore recovery pair." >&2
    if restore_database "$PRIMARY_DB_NAME" "$primary_recovery" \
      && restore_database "$IDENTITY_DB_NAME" "$identity_recovery"; then
      restore_started=false
    else
      echo "Automatic paired recovery failed; application writers remain stopped." >&2
      exit "$exit_code"
    fi
  fi

  if [[ "$apps_stopped" == "true" ]]; then
    start_apps || true
  fi
  exit "$exit_code"
}
trap recover_on_exit EXIT

apps_stopped=true
compose stop frontend backend

BACKUP_TIMESTAMP="$recovery_timestamp" \
DB_CONTAINER="$DB_CONTAINER" \
PRIMARY_DB_NAME="$PRIMARY_DB_NAME" \
IDENTITY_DB_NAME="$IDENTITY_DB_NAME" \
  bash "$SCRIPT_DIR/backup-db-pair.sh" before-pair-restore "$BACKUP_DIR"

verify_backup "$PRIMARY_DB_NAME" "$primary_recovery"
verify_backup "$IDENTITY_DB_NAME" "$identity_recovery"

restore_started=true
restore_database "$PRIMARY_DB_NAME" "$primary_source"
restore_database "$IDENTITY_DB_NAME" "$identity_source"
restore_started=false

start_apps
apps_stopped=false
trap - EXIT

echo "Paired restore completed and all application containers are healthy. Verify the public boundary."
