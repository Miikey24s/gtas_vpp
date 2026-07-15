#!/usr/bin/env bash
set -euo pipefail

DB_CONTAINER="${DB_CONTAINER:-gtas-vpp-db}"
APP_ROOT="${APP_ROOT:-/app/gtas-vpp}"
CONTAINMENT_BUNDLE_DIR="${CONTAINMENT_BUNDLE_DIR:-}"

if [[ ! "$DB_CONTAINER" =~ ^[A-Za-z0-9_.-]+$ ]]; then
  echo "The database container name is invalid." >&2
  exit 2
fi

if [[ "$APP_ROOT" != "/app/gtas-vpp" ]]; then
  echo "The production application root is unexpected." >&2
  exit 2
fi

current_release="$(readlink -f "$APP_ROOT/current")"
case "$current_release" in
  "$APP_ROOT"/releases/*) ;;
  *)
    echo "The current production release link is invalid." >&2
    exit 1
    ;;
esac

bundle_dir="$(readlink -f "$CONTAINMENT_BUNDLE_DIR")"
case "$bundle_dir" in
  /tmp/gtas-vpp-account-containment-*) ;;
  *)
    echo "The containment bundle path is invalid." >&2
    exit 1
    ;;
esac

backup_pair_script="$bundle_dir/backup-db-pair.sh"
backup_member_script="$bundle_dir/backup-db.sh"
if [[ ! -f "$backup_pair_script" || ! -f "$backup_member_script" ]]; then
  echo "The pinned paired-backup bundle is incomplete." >&2
  exit 1
fi

compose_file="$current_release/docker-compose.prod.yml"
env_file="$current_release/.env"
deploy_state="$current_release/deploy-state.env"
if [[ ! -f "$compose_file" || ! -f "$env_file" || ! -f "$deploy_state" ]]; then
  echo "The current release metadata is incomplete." >&2
  exit 1
fi

BE_IMAGE="$(grep '^BE_IMAGE=' "$deploy_state" | tail -n 1 | cut -d= -f2-)"
FE_IMAGE="$(grep '^FE_IMAGE=' "$deploy_state" | tail -n 1 | cut -d= -f2-)"
if [[ -z "$BE_IMAGE" || -z "$FE_IMAGE" ]]; then
  echo "The current application image pair is unavailable." >&2
  exit 1
fi
export BE_IMAGE FE_IMAGE

compose() {
  docker compose --env-file "$env_file" -f "$compose_file" "$@"
}

if [[ "$(docker inspect --format '{{.State.Running}}' "$DB_CONTAINER" 2>/dev/null)" != "true" ]]; then
  echo "The expected production database container is not running." >&2
  exit 1
fi

data_mount_type="$(docker inspect --format '{{range .Mounts}}{{if eq .Destination "/var/opt/mssql"}}{{.Type}}{{end}}{{end}}' "$DB_CONTAINER")"
data_mount_name="$(docker inspect --format '{{range .Mounts}}{{if eq .Destination "/var/opt/mssql"}}{{.Name}}{{end}}{{end}}' "$DB_CONTAINER")"
if [[ "$data_mount_type" != "volume" || -z "$data_mount_name" ]]; then
  echo "The production SQL data volume identity is invalid." >&2
  exit 1
fi

port_bindings="$(docker inspect --format '{{json (index .HostConfig.PortBindings "1433/tcp")}}' "$DB_CONTAINER")"
if [[ "$port_bindings" != *'"HostIp":"127.0.0.1"'* \
   || "$port_bindings" == *'"HostIp":"0.0.0.0"'* \
   || "$port_bindings" == *'"HostIp":"::"'* ]]; then
  echo "The production SQL port binding is unsafe." >&2
  exit 1
fi

is_containment_post_state() {
  local result=""
  result="$(
    docker exec "$DB_CONTAINER" bash -euc '
      /opt/mssql-tools18/bin/sqlcmd \
        -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -b \
        -d GTAS_VPP_LIVE -h -1 -W \
        -Q "SET NOCOUNT ON; SELECT CASE WHEN
          (SELECT COUNT_BIG(*) FROM GTAS_MENU.dbo.tblUsers) = 12
          AND (SELECT COUNT_BIG(*) FROM GTAS_MENU.dbo.tblUsers
               WHERE IsLockedFlg = 1 AND IsInactiveFlg = 1
                 AND PasswordChar LIKE N'''!SEC001-%''') = 12
          AND (SELECT COUNT_BIG(*) FROM GTAS_MENU.dbo.tblUsers
               WHERE ISNULL(IsLockedFlg, 0) = 0
                  OR ISNULL(IsInactiveFlg, 0) = 0) = 0
          AND (SELECT COUNT_BIG(*) FROM dbo.P04_UserGroup) = 12
          AND (SELECT COUNT_BIG(*) FROM dbo.P04_UserGroup
               WHERE IsDeleted = 1) = 12
          THEN 1 ELSE 0 END;"
    ' 2>/dev/null | tr -d '[:space:]'
  )" || return 1
  [[ "$result" == "1" ]]
}

ensure_apps_stopped() {
  compose stop frontend backend
  for container in gtas-vpp-frontend gtas-vpp-backend; do
    if [[ "$(docker inspect --format '{{.State.Running}}' "$container" 2>/dev/null)" == "true" ]]; then
      echo "An application writer is still running." >&2
      return 1
    fi
  done
}

if is_containment_post_state; then
  ensure_apps_stopped
  printf '%s\n' \
    'accounts_active_after=0' \
    'accounts_contained=12' \
    'applications_quiesced_for_key_rotation=1' \
    'containment_already_applied=1' \
    'memberships_active_after=0' \
    'memberships_soft_deleted=12' \
    'paired_backup_verified_before_containment=1'
  exit 0
fi

apps_quiesced=false
recover_on_exit() {
  local exit_code="$?"
  trap - EXIT
  set +e

  if [[ "$apps_quiesced" == "true" ]]; then
    if is_containment_post_state; then
      echo "Containment post-state exists; applications remain stopped for runtime-key rotation." >&2
    else
      echo "Containment did not commit; restarting the previous healthy application pair." >&2
      compose up -d --wait --wait-timeout 180 backend frontend || true
    fi
  fi

  exit "$exit_code"
}
trap recover_on_exit EXIT

ensure_apps_stopped
apps_quiesced=true

backup_timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
BACKUP_TIMESTAMP="$backup_timestamp" \
BACKUP_RETENTION_DAYS="${BACKUP_RETENTION_DAYS:-30}" \
DB_CONTAINER="$DB_CONTAINER" \
  bash "$backup_pair_script" sec001_pre_containment

containment_sql="$(cat <<'SQL'
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

IF DB_NAME() <> N'GTAS_VPP_LIVE'
    THROW 51000, 'Containment connected to an unexpected database.', 1;

IF OBJECT_ID(N'GTAS_MENU.dbo.tblUsers', N'U') IS NULL
   OR OBJECT_ID(N'dbo.P04_UserGroup', N'U') IS NULL
   OR OBJECT_ID(N'dbo.LEX02_CompanyDepartmentLocation', N'U') IS NULL
    THROW 51000, 'Containment schema is incomplete.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @ExpectedAccountDigest varchar(64) =
        'DFC216DE66DF33DC3681D9CAF03140EFC32793B715D0CDB98975AE60568534F9';
    DECLARE @ExpectedMembershipDigest varchar(64) =
        '55FD16BD2AEACE351B7B1AEEE71ADDF5EFC890AA3C26EFD99FF0B09D2738478C';
    DECLARE @AccountCanonical nvarchar(max);
    DECLARE @MembershipCanonical nvarchar(max);
    DECLARE @AccountDigest varchar(64);
    DECLARE @MembershipDigest varchar(64);
    DECLARE @AccountCount bigint;
    DECLARE @MembershipCount bigint;
    DECLARE @ActiveAccountCount bigint;
    DECLARE @ActiveMembershipCount bigint;
    DECLARE @BlankVerifierCount bigint;
    DECLARE @DistinctVerifierCount bigint;
    DECLARE @AccountReservedControlRows bigint;
    DECLARE @MembershipReservedControlRows bigint;
    DECLARE @UpdatedAccounts int;
    DECLARE @UpdatedMemberships int;

    SELECT
        @AccountCount = COUNT_BIG(*),
        @ActiveAccountCount = COALESCE(SUM(CONVERT(bigint,
            CASE WHEN ISNULL(u.IsInactiveFlg, 0) = 0
                   AND ISNULL(u.IsLockedFlg, 0) = 0
                 THEN 1 ELSE 0 END)), 0),
        @BlankVerifierCount = COALESCE(SUM(CONVERT(bigint,
            CASE WHEN NULLIF(LTRIM(RTRIM(u.PasswordChar)), N'') IS NULL
                 THEN 1 ELSE 0 END)), 0),
        @AccountReservedControlRows = COALESCE(SUM(CONVERT(bigint,
            CASE WHEN
                CHARINDEX(NCHAR(28), CONCAT(u.UserLogin, u.FullName,
                    u.EmailAddress1, u.EmailAddress2, u.GoogleEmail,
                    u.PhoneNo1, u.PhoneNo2, u.DepartmentCode,
                    u.MemberCompanyName)) > 0
                OR CHARINDEX(NCHAR(29), CONCAT(u.UserLogin, u.FullName,
                    u.EmailAddress1, u.EmailAddress2, u.GoogleEmail,
                    u.PhoneNo1, u.PhoneNo2, u.DepartmentCode,
                    u.MemberCompanyName)) > 0
                OR CHARINDEX(NCHAR(31), CONCAT(u.UserLogin, u.FullName,
                    u.EmailAddress1, u.EmailAddress2, u.GoogleEmail,
                    u.PhoneNo1, u.PhoneNo2, u.DepartmentCode,
                    u.MemberCompanyName)) > 0
                THEN 1 ELSE 0 END)), 0)
    FROM GTAS_MENU.dbo.tblUsers AS u WITH (UPDLOCK, HOLDLOCK);

    SELECT @DistinctVerifierCount = COUNT_BIG(*)
    FROM (
        SELECT u.PasswordChar
        FROM GTAS_MENU.dbo.tblUsers AS u WITH (UPDLOCK, HOLDLOCK)
        GROUP BY u.PasswordChar
    ) AS verifier_cohorts;

    SELECT @AccountCanonical = CONVERT(nvarchar(max),
        STRING_AGG(rows.RowCanonical, NCHAR(28))
            WITHIN GROUP (ORDER BY rows.UserID))
    FROM (
        SELECT
            u.UserID,
            RowCanonical = CONVERT(nvarchar(max), CONCAT(
                CONVERT(nvarchar(20), u.UserID), NCHAR(31),
                COALESCE(u.UserLogin, NCHAR(29)), NCHAR(31),
                COALESCE(u.FullName, NCHAR(29)), NCHAR(31),
                COALESCE(u.EmailAddress1, NCHAR(29)), NCHAR(31),
                COALESCE(u.EmailAddress2, NCHAR(29)), NCHAR(31),
                COALESCE(u.GoogleEmail, NCHAR(29)), NCHAR(31),
                COALESCE(u.PhoneNo1, NCHAR(29)), NCHAR(31),
                COALESCE(u.PhoneNo2, NCHAR(29)), NCHAR(31),
                COALESCE(CONVERT(nvarchar(1), u.IsInactiveFlg), NCHAR(29)), NCHAR(31),
                COALESCE(CONVERT(nvarchar(1), u.IsLockedFlg), NCHAR(29)), NCHAR(31),
                COALESCE(CONVERT(nvarchar(30), u.MemberCompanyCode), NCHAR(29)), NCHAR(31),
                COALESCE(u.DepartmentCode, NCHAR(29)), NCHAR(31),
                COALESCE(u.MemberCompanyName, NCHAR(29))))
        FROM GTAS_MENU.dbo.tblUsers AS u WITH (UPDLOCK, HOLDLOCK)
    ) AS rows;

    SET @AccountDigest = CONVERT(varchar(64),
        HASHBYTES(N'SHA2_256', @AccountCanonical), 2);

    SELECT
        @MembershipCount = COUNT_BIG(*),
        @ActiveMembershipCount = COALESCE(SUM(CONVERT(bigint,
            CASE WHEN p.IsDeleted = 0 THEN 1 ELSE 0 END)), 0),
        @MembershipReservedControlRows = COALESCE(SUM(CONVERT(bigint,
            CASE WHEN
                CHARINDEX(NCHAR(28), COALESCE(d.LEX02Code, N'')) > 0
                OR CHARINDEX(NCHAR(29), COALESCE(d.LEX02Code, N'')) > 0
                OR CHARINDEX(NCHAR(31), COALESCE(d.LEX02Code, N'')) > 0
                THEN 1 ELSE 0 END)), 0)
    FROM dbo.P04_UserGroup AS p WITH (UPDLOCK, HOLDLOCK)
    LEFT JOIN dbo.LEX02_CompanyDepartmentLocation AS d WITH (HOLDLOCK)
        ON d.Id = p.LEX02_CompanyDepartmentLocationId;

    SELECT @MembershipCanonical = CONVERT(nvarchar(max),
        STRING_AGG(rows.RowCanonical, NCHAR(28))
            WITHIN GROUP (ORDER BY rows.UserId, rows.P02_GroupId,
                rows.LEX02_CompanyDepartmentLocationId))
    FROM (
        SELECT
            p.UserId,
            p.P02_GroupId,
            p.LEX02_CompanyDepartmentLocationId,
            RowCanonical = CONVERT(nvarchar(max), CONCAT(
                CONVERT(nvarchar(20), p.UserId), NCHAR(31),
                LOWER(CONVERT(nvarchar(36), p.P02_GroupId)), NCHAR(31),
                COALESCE(d.LEX02Code, NCHAR(29)), NCHAR(31),
                COALESCE(CONVERT(nvarchar(1), p.IsDeleted), NCHAR(29))))
        FROM dbo.P04_UserGroup AS p WITH (UPDLOCK, HOLDLOCK)
        LEFT JOIN dbo.LEX02_CompanyDepartmentLocation AS d WITH (HOLDLOCK)
            ON d.Id = p.LEX02_CompanyDepartmentLocationId
    ) AS rows;

    SET @MembershipDigest = CONVERT(varchar(64),
        HASHBYTES(N'SHA2_256', @MembershipCanonical), 2);

    IF @AccountCount <> 12
       OR @MembershipCount <> 12
       OR @ActiveAccountCount <> 12
       OR @ActiveMembershipCount <> 12
       OR @BlankVerifierCount <> 0
       OR @DistinctVerifierCount <> 1
       OR @AccountReservedControlRows <> 0
       OR @MembershipReservedControlRows <> 0
       OR @AccountDigest <> @ExpectedAccountDigest
       OR @MembershipDigest <> @ExpectedMembershipDigest
        THROW 51001, 'Exact historical demo manifest changed; containment refused.', 1;

    UPDATE u
    SET
        u.IsLockedFlg = 1,
        u.IsInactiveFlg = 1,
        u.PasswordChar = CONCAT(N'!SEC001-', CONVERT(nvarchar(20), u.UserID),
            N'-', CONVERT(nvarchar(36), NEWID()), N'!')
    FROM GTAS_MENU.dbo.tblUsers AS u;

    SET @UpdatedAccounts = @@ROWCOUNT;
    IF @UpdatedAccounts <> 12
        THROW 51002, 'Unexpected account containment row count.', 1;

    UPDATE p
    SET
        p.IsDeleted = 1,
        p.UpdateUserId = 0,
        p.UpdateDate = SYSUTCDATETIME()
    FROM dbo.P04_UserGroup AS p;

    SET @UpdatedMemberships = @@ROWCOUNT;
    IF @UpdatedMemberships <> 12
        THROW 51003, 'Unexpected membership containment row count.', 1;

    IF (SELECT COUNT_BIG(*) FROM GTAS_MENU.dbo.tblUsers
        WHERE IsLockedFlg = 1 AND IsInactiveFlg = 1
          AND PasswordChar LIKE N'!SEC001-%') <> 12
       OR (SELECT COUNT_BIG(*) FROM GTAS_MENU.dbo.tblUsers
           WHERE ISNULL(IsLockedFlg, 0) = 0
              OR ISNULL(IsInactiveFlg, 0) = 0) <> 0
       OR (SELECT COUNT_BIG(*) FROM dbo.P04_UserGroup
           WHERE IsDeleted = 0) <> 0
       OR (SELECT COUNT_BIG(*) FROM dbo.P04_UserGroup
           WHERE IsDeleted = 1) <> 12
        THROW 51004, 'Containment postcondition failed.', 1;

    COMMIT TRANSACTION;

    SELECT metrics.Metric, metrics.Value
    FROM (VALUES
        (N'accounts_contained', CONVERT(bigint, @UpdatedAccounts)),
        (N'accounts_active_after', CONVERT(bigint, 0)),
        (N'applications_quiesced_for_key_rotation', CONVERT(bigint, 1)),
        (N'containment_already_applied', CONVERT(bigint, 0)),
        (N'memberships_soft_deleted', CONVERT(bigint, @UpdatedMemberships)),
        (N'memberships_active_after', CONVERT(bigint, 0)),
        (N'paired_backup_verified_before_containment', CONVERT(bigint, 1))
    ) AS metrics(Metric, Value)
    ORDER BY metrics.Metric;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
SQL
)"

containment_output="$(
  printf '%s\n' "$containment_sql" |
    docker exec -i "$DB_CONTAINER" bash -euc '
      active_password="${MSSQL_SA_PASSWORD:?MSSQL_SA_PASSWORD is unavailable in the database container}"
      /opt/mssql-tools18/bin/sqlcmd \
        -S localhost -U sa -P "$active_password" -C -b \
        -d GTAS_VPP_LIVE -h -1 -W -s "=" -w 256 -i /dev/stdin
    '
)"

declare -A metrics=()
while IFS= read -r line; do
  line="${line%$'\r'}"
  [[ -z "$line" ]] && continue
  if [[ ! "$line" =~ ^([a-z0-9_]+)=([0-9]+)$ ]]; then
    echo "Containment returned an unexpected output shape." >&2
    exit 1
  fi
  metrics["${BASH_REMATCH[1]}"]="${BASH_REMATCH[2]}"
done <<< "$containment_output"

expected_metrics=(
  accounts_contained=12
  accounts_active_after=0
  applications_quiesced_for_key_rotation=1
  containment_already_applied=0
  memberships_soft_deleted=12
  memberships_active_after=0
  paired_backup_verified_before_containment=1
)

for expectation in "${expected_metrics[@]}"; do
  metric="${expectation%%=*}"
  expected="${expectation#*=}"
  if [[ "${metrics[$metric]:-missing}" != "$expected" ]]; then
    echo "Containment did not satisfy a required postcondition." >&2
    exit 1
  fi
done

trap - EXIT
for metric in $(printf '%s\n' "${!metrics[@]}" | sort); do
  printf '%s=%s\n' "$metric" "${metrics[$metric]}"
done
