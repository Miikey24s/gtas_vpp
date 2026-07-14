#!/usr/bin/env bash
set -euo pipefail

DB_CONTAINER="${DB_CONTAINER:-gtas-vpp-db}"
DB_NAME="${DB_NAME:-GTAS_VPP_LIVE}"
BACKUP_LABEL="${1:-manual}"
BACKUP_DIR="${2:-/var/opt/mssql/backup}"
BACKUP_RETENTION_DAYS="${BACKUP_RETENTION_DAYS:-14}"

if [[ ! "$DB_NAME" =~ ^[A-Za-z0-9_]+$ ]]; then
  echo "Unsafe database name: $DB_NAME" >&2
  exit 2
fi

if [[ ! "$BACKUP_LABEL" =~ ^[A-Za-z0-9_-]+$ ]]; then
  echo "Backup label may contain only letters, numbers, underscores, and hyphens." >&2
  exit 2
fi

case "$BACKUP_DIR" in
  /var/opt/mssql/backup|/var/opt/mssql/data)
    ;;
  *)
    echo "Backup directory is outside the approved SQL Server paths." >&2
    exit 2
    ;;
esac

timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
backup_file="${BACKUP_DIR}/${DB_NAME}_${BACKUP_LABEL}_${timestamp}.bak"

docker exec -u 0 "$DB_CONTAINER" mkdir -p "$BACKUP_DIR"
docker exec -u 0 "$DB_CONTAINER" chown 10001:0 "$BACKUP_DIR"

docker exec \
  -e BACKUP_FILE="$backup_file" \
  -e DB_NAME="$DB_NAME" \
  -e DB_PASSWORD="${DB_PASSWORD:-}" \
  "$DB_CONTAINER" \
  bash -euc '
    active_password="${DB_PASSWORD:-$MSSQL_SA_PASSWORD}"
    /opt/mssql-tools18/bin/sqlcmd \
      -S localhost -U sa -P "$active_password" -C -b \
      -Q "BACKUP DATABASE [$DB_NAME] TO DISK = N'\''$BACKUP_FILE'\'' WITH COPY_ONLY, COMPRESSION, CHECKSUM, INIT; RESTORE VERIFYONLY FROM DISK = N'\''$BACKUP_FILE'\'' WITH CHECKSUM;"
  '

if [[ "$BACKUP_DIR" == "/var/opt/mssql/backup" ]]; then
  docker exec -u 0 "$DB_CONTAINER" \
    find "$BACKUP_DIR" -maxdepth 1 -type f -name "${DB_NAME}_*.bak" \
      -mtime "+${BACKUP_RETENTION_DAYS}" -delete
fi

echo "Verified database backup: $backup_file"
