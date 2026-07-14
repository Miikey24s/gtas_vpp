#!/usr/bin/env bash
set -Eeuo pipefail

COMPOSE_FILE="${COMPOSE_FILE:-docker-compose.prod.yml}"
DB_CONTAINER="${DB_CONTAINER:-gtas-vpp-db}"
DB_NAME="${DB_NAME:-GTAS_VPP_LIVE}"
BACKUP_NAME="${1:-}"

if [[ "${RESTORE_CONFIRM:-}" != "$DB_NAME" ]]; then
  echo "Refusing restore. Set RESTORE_CONFIRM=$DB_NAME for this command only." >&2
  exit 2
fi

if [[ ! "$BACKUP_NAME" =~ ^[A-Za-z0-9_.-]+\.bak$ ]]; then
  echo "Usage: RESTORE_CONFIRM=$DB_NAME $0 <backup-file-name.bak>" >&2
  exit 2
fi

if [[ ! -f deploy-state.env ]]; then
  echo "deploy-state.env is required to restart the current application images." >&2
  exit 2
fi

BE_IMAGE="$(grep '^BE_IMAGE=' deploy-state.env | tail -n 1 | cut -d= -f2-)"
FE_IMAGE="$(grep '^FE_IMAGE=' deploy-state.env | tail -n 1 | cut -d= -f2-)"
export BE_IMAGE FE_IMAGE

if [[ -z "$BE_IMAGE" || -z "$FE_IMAGE" ]]; then
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

bash deploy/backup-db.sh before-restore
docker compose -f "$COMPOSE_FILE" stop frontend backend

restore_failed=true
recover_database() {
  if [[ "$restore_failed" == "true" ]]; then
    docker exec -e DB_NAME="$DB_NAME" "$DB_CONTAINER" bash -euc '
      /opt/mssql-tools18/bin/sqlcmd \
        -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -b \
        -Q "IF DB_ID(N'\''$DB_NAME'\'') IS NOT NULL ALTER DATABASE [$DB_NAME] SET MULTI_USER;"
    ' || true
  fi
}
trap recover_database EXIT

docker exec \
  -e BACKUP_FILE="$backup_path" \
  -e DB_NAME="$DB_NAME" \
  "$DB_CONTAINER" \
  bash -euc '
    /opt/mssql-tools18/bin/sqlcmd \
      -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -b \
      -Q "ALTER DATABASE [$DB_NAME] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; RESTORE DATABASE [$DB_NAME] FROM DISK = N'\''$BACKUP_FILE'\'' WITH REPLACE, RECOVERY; ALTER DATABASE [$DB_NAME] SET MULTI_USER;"
  '

restore_failed=false
trap - EXIT

docker compose -f "$COMPOSE_FILE" up -d backend frontend
echo "Restore completed. Verify both container health checks and the public application."
