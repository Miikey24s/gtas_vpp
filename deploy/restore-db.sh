#!/usr/bin/env bash
set -Eeuo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
COMPOSE_FILE="${COMPOSE_FILE:-docker-compose.prod.yml}"
DB_CONTAINER="${DB_CONTAINER:-gtas-vpp-db}"
DB_NAME="${DB_NAME:-GTAS_VPP_LIVE}"
BACKUP_NAME="${1:-}"

if [[ ! "$DB_NAME" =~ ^[A-Za-z0-9_]+$ ]]; then
  echo "Unsafe database name: $DB_NAME" >&2
  exit 2
fi

if [[ "${RESTORE_CONFIRM:-}" != "$DB_NAME" ]]; then
  echo "Refusing restore. Set RESTORE_CONFIRM=$DB_NAME for this command only." >&2
  exit 2
fi

if [[ ! "$BACKUP_NAME" =~ ^${DB_NAME}_[A-Za-z0-9_-]+_[0-9]{8}T[0-9]{6}Z\.bak$ ]]; then
  echo "Backup file must be a verified $DB_NAME backup created by backup-db.sh." >&2
  echo "Usage: RESTORE_CONFIRM=$DB_NAME $0 ${DB_NAME}_<label>_<UTC-timestamp>.bak" >&2
  exit 2
fi

if [[ ! -f deploy-state.env ]]; then
  echo "deploy-state.env is required to restart the current application images." >&2
  exit 2
fi

BE_IMAGE="$(grep '^BE_IMAGE=' deploy-state.env | tail -n 1 | cut -d= -f2-)"
FE_IMAGE="$(grep '^FE_IMAGE=' deploy-state.env | tail -n 1 | cut -d= -f2-)"
REACT_FE_IMAGE="$(grep '^REACT_FE_IMAGE=' deploy-state.env | tail -n 1 | cut -d= -f2-)"
export BE_IMAGE FE_IMAGE REACT_FE_IMAGE

if [[ -z "$BE_IMAGE" || -z "$FE_IMAGE" || -z "$REACT_FE_IMAGE" ]]; then
  echo "Current application image metadata is incomplete." >&2
  exit 2
fi

backup_path="/var/opt/mssql/backup/$BACKUP_NAME"
docker exec "$DB_CONTAINER" test -f "$backup_path"

docker exec \
  -e BACKUP_FILE="$backup_path" \
  -e DB_NAME="$DB_NAME" \
  "$DB_CONTAINER" \
  bash -euc '
    /opt/mssql-tools18/bin/sqlcmd \
      -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -b \
      -Q "RESTORE VERIFYONLY FROM DISK = N'\''$BACKUP_FILE'\'' WITH CHECKSUM;"
  '

apps_stopped=false
restore_in_progress=false
recover_on_exit() {
  local exit_code="$?"
  trap - EXIT
  set +e

  if [[ "$restore_in_progress" == "true" ]]; then
    docker exec -e DB_NAME="$DB_NAME" "$DB_CONTAINER" bash -euc '
      /opt/mssql-tools18/bin/sqlcmd \
        -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -b \
        -Q "IF DB_ID(N'\''$DB_NAME'\'') IS NOT NULL ALTER DATABASE [$DB_NAME] SET MULTI_USER;"
    ' || true
  fi

  if [[ "$apps_stopped" == "true" ]]; then
    docker compose -f "$COMPOSE_FILE" up -d backend frontend react-frontend || true
  fi

  exit "$exit_code"
}
trap recover_on_exit EXIT

# Quiesce application writes before capturing the paired recovery point.
apps_stopped=true
docker compose -f "$COMPOSE_FILE" stop react-frontend frontend backend
bash "$SCRIPT_DIR/backup-db-pair.sh" before-restore

restore_in_progress=true
docker exec \
  -e BACKUP_FILE="$backup_path" \
  -e DB_NAME="$DB_NAME" \
  "$DB_CONTAINER" \
  bash -euc '
    /opt/mssql-tools18/bin/sqlcmd \
      -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -b \
      -Q "ALTER DATABASE [$DB_NAME] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; RESTORE DATABASE [$DB_NAME] FROM DISK = N'\''$BACKUP_FILE'\'' WITH REPLACE, RECOVERY; ALTER DATABASE [$DB_NAME] SET MULTI_USER;"
  '

restore_in_progress=false
docker compose -f "$COMPOSE_FILE" up -d --wait --wait-timeout 180 backend frontend react-frontend
apps_stopped=false
trap - EXIT

echo "Restore completed and all application containers are healthy. Verify the public application."
