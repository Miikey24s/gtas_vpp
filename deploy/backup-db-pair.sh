#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
DB_CONTAINER="${DB_CONTAINER:-gtas-vpp-db}"
PRIMARY_DB_NAME="${PRIMARY_DB_NAME:-GTAS_VPP_LIVE}"
IDENTITY_DB_NAME="${IDENTITY_DB_NAME:-GTAS_MENU}"
BACKUP_LABEL="${1:-manual}"
BACKUP_DIR="${2:-/var/opt/mssql/backup}"
BACKUP_TIMESTAMP="${BACKUP_TIMESTAMP:-$(date -u +%Y%m%dT%H%M%SZ)}"

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

# Fail before writing either member when the required logical database pair is incomplete.
docker exec \
  -e PRIMARY_DB_NAME="$PRIMARY_DB_NAME" \
  -e IDENTITY_DB_NAME="$IDENTITY_DB_NAME" \
  -e DB_PASSWORD="${DB_PASSWORD:-}" \
  "$DB_CONTAINER" \
  bash -euc '
    active_password="${DB_PASSWORD:-$MSSQL_SA_PASSWORD}"
    /opt/mssql-tools18/bin/sqlcmd \
      -S localhost -U sa -P "$active_password" -C -b \
      -v PRIMARY_DB="$PRIMARY_DB_NAME" IDENTITY_DB="$IDENTITY_DB_NAME" \
      -Q "IF DB_ID(N'\''\$(PRIMARY_DB)'\'') IS NULL BEGIN RAISERROR ('\''Required primary database is missing.'\'', 16, 1); RETURN; END; IF DB_ID(N'\''\$(IDENTITY_DB)'\'') IS NULL BEGIN RAISERROR ('\''Required identity database is missing.'\'', 16, 1); RETURN; END;" \
      >/dev/null
  '

for database_name in "$PRIMARY_DB_NAME" "$IDENTITY_DB_NAME"; do
  DB_NAME="$database_name" \
  BACKUP_TIMESTAMP="$BACKUP_TIMESTAMP" \
    bash "$SCRIPT_DIR/backup-db.sh" "$BACKUP_LABEL" "$BACKUP_DIR"
done

echo "Verified database backup set: $BACKUP_LABEL/$BACKUP_TIMESTAMP (2 databases)."
