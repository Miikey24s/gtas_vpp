#!/usr/bin/env bash
set -euo pipefail

DB_CONTAINER="${DB_CONTAINER:-gtas-vpp-db}"

if [[ ! "$DB_CONTAINER" =~ ^[A-Za-z0-9_.-]+$ ]]; then
  echo "The database container name is invalid." >&2
  exit 2
fi

if [[ "$(docker inspect --format '{{.State.Running}}' "$DB_CONTAINER" 2>/dev/null)" != "true" ]]; then
  echo "The expected production database container is not running." >&2
  exit 1
fi

audit_sql="$(cat <<'SQL'
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

IF DB_NAME() <> N'GTAS_VPP_LIVE'
    THROW 51000, 'The account audit is connected to an unexpected database.', 1;

IF NOT EXISTS (
    SELECT 1 FROM sys.databases
    WHERE name = N'GTAS_MENU' AND state = 0
)
    THROW 51000, 'The identity database is missing or offline.', 1;

IF OBJECT_ID(N'GTAS_MENU.dbo.tblUsers', N'U') IS NULL
   OR OBJECT_ID(N'dbo.P04_UserGroup', N'U') IS NULL
   OR OBJECT_ID(N'dbo.P02_Group', N'U') IS NULL
   OR OBJECT_ID(N'dbo.LEX02_CompanyDepartmentLocation', N'U') IS NULL
    THROW 51000, 'Required account audit tables are missing.', 1;

IF COL_LENGTH(N'GTAS_MENU.dbo.tblUsers', N'UserID') IS NULL
   OR COL_LENGTH(N'GTAS_MENU.dbo.tblUsers', N'UserLogin') IS NULL
   OR COL_LENGTH(N'GTAS_MENU.dbo.tblUsers', N'PasswordChar') IS NULL
   OR COL_LENGTH(N'GTAS_MENU.dbo.tblUsers', N'FullName') IS NULL
   OR COL_LENGTH(N'GTAS_MENU.dbo.tblUsers', N'EmailAddress1') IS NULL
   OR COL_LENGTH(N'GTAS_MENU.dbo.tblUsers', N'EmailAddress2') IS NULL
   OR COL_LENGTH(N'GTAS_MENU.dbo.tblUsers', N'GoogleEmail') IS NULL
   OR COL_LENGTH(N'GTAS_MENU.dbo.tblUsers', N'PhoneNo1') IS NULL
   OR COL_LENGTH(N'GTAS_MENU.dbo.tblUsers', N'PhoneNo2') IS NULL
   OR COL_LENGTH(N'GTAS_MENU.dbo.tblUsers', N'IsInactiveFlg') IS NULL
   OR COL_LENGTH(N'GTAS_MENU.dbo.tblUsers', N'IsLockedFlg') IS NULL
   OR COL_LENGTH(N'GTAS_MENU.dbo.tblUsers', N'MemberCompanyCode') IS NULL
   OR COL_LENGTH(N'GTAS_MENU.dbo.tblUsers', N'DepartmentCode') IS NULL
   OR COL_LENGTH(N'GTAS_MENU.dbo.tblUsers', N'MemberCompanyName') IS NULL
   OR COL_LENGTH(N'dbo.P04_UserGroup', N'UserId') IS NULL
   OR COL_LENGTH(N'dbo.P04_UserGroup', N'P02_GroupId') IS NULL
   OR COL_LENGTH(N'dbo.P04_UserGroup', N'LEX02_CompanyDepartmentLocationId') IS NULL
   OR COL_LENGTH(N'dbo.P04_UserGroup', N'IsDeleted') IS NULL
   OR COL_LENGTH(N'dbo.P02_Group', N'Id') IS NULL
   OR COL_LENGTH(N'dbo.P02_Group', N'GroupName') IS NULL
   OR COL_LENGTH(N'dbo.P02_Group', N'IsDeleted') IS NULL
   OR COL_LENGTH(N'dbo.LEX02_CompanyDepartmentLocation', N'Id') IS NULL
   OR COL_LENGTH(N'dbo.LEX02_CompanyDepartmentLocation', N'LEX02Code') IS NULL
   OR COL_LENGTH(N'dbo.LEX02_CompanyDepartmentLocation', N'IsDeleted') IS NULL
    THROW 51000, 'Required account audit columns are missing.', 1;

;WITH Accounts AS
(
    SELECT
        u.UserID,
        NormalizedLogin = NULLIF(
            LOWER(LTRIM(RTRIM(u.UserLogin COLLATE DATABASE_DEFAULT))), N''),
        u.UserLogin,
        u.PasswordChar,
        u.FullName,
        u.EmailAddress1,
        u.EmailAddress2,
        u.GoogleEmail,
        u.PhoneNo1,
        u.PhoneNo2,
        u.IsInactiveFlg,
        u.IsLockedFlg,
        u.MemberCompanyCode,
        u.DepartmentCode,
        u.MemberCompanyName,
        IsActive = IIF(ISNULL(u.IsInactiveFlg, 0) = 0
            AND ISNULL(u.IsLockedFlg, 0) = 0, 1, 0),
        IsLocked = IIF(u.IsLockedFlg = 1, 1, 0),
        IsInactive = IIF(u.IsInactiveFlg = 1, 1, 0),
        HasNullStatus = IIF(u.IsInactiveFlg IS NULL
            OR u.IsLockedFlg IS NULL, 1, 0),
        MissingCompany = IIF(u.MemberCompanyCode IS NULL, 1, 0),
        MissingDepartment = IIF(
            NULLIF(LTRIM(RTRIM(u.DepartmentCode)), N'') IS NULL, 1, 0),
        MissingVerifier = IIF(
            NULLIF(LTRIM(RTRIM(u.PasswordChar)), N'') IS NULL, 1, 0)
    FROM GTAS_MENU.dbo.tblUsers AS u
),
AccountCanonicalRows AS
(
    SELECT
        a.UserID,
        HasReservedControl = CASE WHEN
            CHARINDEX(NCHAR(28), CONCAT(a.UserLogin, a.FullName,
                a.EmailAddress1, a.EmailAddress2, a.GoogleEmail,
                a.PhoneNo1, a.PhoneNo2, a.DepartmentCode,
                a.MemberCompanyName)) > 0
            OR CHARINDEX(NCHAR(29), CONCAT(a.UserLogin, a.FullName,
                a.EmailAddress1, a.EmailAddress2, a.GoogleEmail,
                a.PhoneNo1, a.PhoneNo2, a.DepartmentCode,
                a.MemberCompanyName)) > 0
            OR CHARINDEX(NCHAR(31), CONCAT(a.UserLogin, a.FullName,
                a.EmailAddress1, a.EmailAddress2, a.GoogleEmail,
                a.PhoneNo1, a.PhoneNo2, a.DepartmentCode,
                a.MemberCompanyName)) > 0
            THEN CONVERT(bigint, 1) ELSE CONVERT(bigint, 0) END,
        RowCanonical = CONVERT(nvarchar(max), CONCAT(
            CONVERT(nvarchar(20), a.UserID), NCHAR(31),
            COALESCE(a.UserLogin, NCHAR(29)), NCHAR(31),
            COALESCE(a.FullName, NCHAR(29)), NCHAR(31),
            COALESCE(a.EmailAddress1, NCHAR(29)), NCHAR(31),
            COALESCE(a.EmailAddress2, NCHAR(29)), NCHAR(31),
            COALESCE(a.GoogleEmail, NCHAR(29)), NCHAR(31),
            COALESCE(a.PhoneNo1, NCHAR(29)), NCHAR(31),
            COALESCE(a.PhoneNo2, NCHAR(29)), NCHAR(31),
            COALESCE(CONVERT(nvarchar(1), a.IsInactiveFlg), NCHAR(29)), NCHAR(31),
            COALESCE(CONVERT(nvarchar(1), a.IsLockedFlg), NCHAR(29)), NCHAR(31),
            COALESCE(CONVERT(nvarchar(30), a.MemberCompanyCode), NCHAR(29)), NCHAR(31),
            COALESCE(a.DepartmentCode, NCHAR(29)), NCHAR(31),
            COALESCE(a.MemberCompanyName, NCHAR(29))))
    FROM Accounts AS a
),
AccountManifest AS
(
    SELECT
        ReservedControlRows = COALESCE(SUM(HasReservedControl), 0),
        ManifestDigest = CONVERT(varchar(64), HASHBYTES(N'SHA2_256',
            CONVERT(nvarchar(max), STRING_AGG(RowCanonical, NCHAR(28))
                WITHIN GROUP (ORDER BY UserID))), 2)
    FROM AccountCanonicalRows
),
VerifierCohorts AS
(
    SELECT PasswordChar, CohortSize = COUNT_BIG(*)
    FROM Accounts
    GROUP BY PasswordChar
),
LoginCohorts AS
(
    SELECT NormalizedLogin, CohortSize = COUNT_BIG(*)
    FROM Accounts
    WHERE NormalizedLogin IS NOT NULL
    GROUP BY NormalizedLogin
),
ActiveMappings AS
(
    SELECT
        p.UserId,
        UserOrphan = IIF(a.UserID IS NULL, 1, 0),
        GroupOrphan = IIF(g.Id IS NULL, 1, 0),
        LocationOrphan = IIF(d.Id IS NULL, 1, 0),
        GroupDeleted = IIF(g.IsDeleted = 1, 1, 0),
        LocationDeleted = IIF(d.IsDeleted = 1, 1, 0),
        RoleClass = CASE LOWER(LTRIM(RTRIM(
            g.GroupName COLLATE DATABASE_DEFAULT)))
            WHEN N'admin' THEN 1
            WHEN N'user' THEN 2
            ELSE 3
        END
    FROM dbo.P04_UserGroup AS p
    LEFT JOIN Accounts AS a ON a.UserID = p.UserId
    LEFT JOIN dbo.P02_Group AS g ON g.Id = p.P02_GroupId
    LEFT JOIN dbo.LEX02_CompanyDepartmentLocation AS d
        ON d.Id = p.LEX02_CompanyDepartmentLocationId
    WHERE p.IsDeleted = 0
),
AccountGroupCounts AS
(
    SELECT a.UserID, ActiveMappingCount = COUNT_BIG(p.Id)
    FROM Accounts AS a
    LEFT JOIN dbo.P04_UserGroup AS p
        ON p.UserId = a.UserID AND p.IsDeleted = 0
    GROUP BY a.UserID
),
MembershipCanonicalRows AS
(
    SELECT
        p.UserId,
        p.P02_GroupId,
        p.LEX02_CompanyDepartmentLocationId,
        HasReservedControl = CASE WHEN
            CHARINDEX(NCHAR(28), COALESCE(d.LEX02Code, N'')) > 0
            OR CHARINDEX(NCHAR(29), COALESCE(d.LEX02Code, N'')) > 0
            OR CHARINDEX(NCHAR(31), COALESCE(d.LEX02Code, N'')) > 0
            THEN CONVERT(bigint, 1) ELSE CONVERT(bigint, 0) END,
        RowCanonical = CONVERT(nvarchar(max), CONCAT(
            CONVERT(nvarchar(20), p.UserId), NCHAR(31),
            LOWER(CONVERT(nvarchar(36), p.P02_GroupId)), NCHAR(31),
            COALESCE(d.LEX02Code, NCHAR(29)), NCHAR(31),
            COALESCE(CONVERT(nvarchar(1), p.IsDeleted), NCHAR(29))))
    FROM dbo.P04_UserGroup AS p
    LEFT JOIN dbo.LEX02_CompanyDepartmentLocation AS d
        ON d.Id = p.LEX02_CompanyDepartmentLocationId
),
MembershipManifest AS
(
    SELECT
        TotalMappings = COUNT_BIG(*),
        ReservedControlRows = COALESCE(SUM(HasReservedControl), 0),
        ManifestDigest = CONVERT(varchar(64), HASHBYTES(N'SHA2_256',
            CONVERT(nvarchar(max), STRING_AGG(RowCanonical, NCHAR(28))
                WITHIN GROUP (ORDER BY UserId, P02_GroupId,
                    LEX02_CompanyDepartmentLocationId))), 2)
    FROM MembershipCanonicalRows
),
Metrics AS
(
    SELECT
        TotalAccounts = (SELECT COUNT_BIG(*) FROM Accounts),
        ActiveAccounts = (SELECT COALESCE(SUM(CONVERT(bigint, IsActive)), 0) FROM Accounts),
        LockedAccounts = (SELECT COALESCE(SUM(CONVERT(bigint, IsLocked)), 0) FROM Accounts),
        InactiveAccounts = (SELECT COALESCE(SUM(CONVERT(bigint, IsInactive)), 0) FROM Accounts),
        NullStatusFlagAccounts = (SELECT COALESCE(SUM(CONVERT(bigint, HasNullStatus)), 0) FROM Accounts),
        DistinctVerifierCount = (SELECT COUNT_BIG(*) FROM VerifierCohorts),
        LargestVerifierCohort = (SELECT COALESCE(MAX(CohortSize), 0) FROM VerifierCohorts),
        NullOrEmptyVerifierAccounts = (SELECT COALESCE(SUM(CONVERT(bigint, MissingVerifier)), 0) FROM Accounts),
        NullOrEmptyNormalizedLoginAccounts = (SELECT COUNT_BIG(*) FROM Accounts WHERE NormalizedLogin IS NULL),
        NormalizedLoginDuplicateCohorts = (SELECT COUNT_BIG(*) FROM LoginCohorts WHERE CohortSize > 1),
        AccountsInDuplicateLoginCohorts = (SELECT COALESCE(SUM(CohortSize), 0) FROM LoginCohorts WHERE CohortSize > 1),
        MissingCompanyAccounts = (SELECT COALESCE(SUM(CONVERT(bigint, MissingCompany)), 0) FROM Accounts),
        MissingDepartmentAccounts = (SELECT COALESCE(SUM(CONVERT(bigint, MissingDepartment)), 0) FROM Accounts),
        ActiveP04Mappings = (SELECT COUNT_BIG(*) FROM ActiveMappings),
        DistinctP04MappedUsers = (SELECT COUNT_BIG(*) FROM (SELECT UserId FROM ActiveMappings GROUP BY UserId) AS mapped),
        ActiveP04UserOrphans = (SELECT COUNT_BIG(*) FROM ActiveMappings WHERE UserOrphan = 1),
        ActiveP04GroupOrphans = (SELECT COUNT_BIG(*) FROM ActiveMappings WHERE GroupOrphan = 1),
        ActiveP04LocationOrphans = (SELECT COUNT_BIG(*) FROM ActiveMappings WHERE LocationOrphan = 1),
        ActiveP04DeletedGroupReferences = (SELECT COUNT_BIG(*) FROM ActiveMappings WHERE GroupDeleted = 1),
        ActiveP04DeletedLocationReferences = (SELECT COUNT_BIG(*) FROM ActiveMappings WHERE LocationDeleted = 1),
        AccountsWithZeroActiveP04 = (SELECT COUNT_BIG(*) FROM AccountGroupCounts WHERE ActiveMappingCount = 0),
        AccountsWithMultipleActiveP04 = (SELECT COUNT_BIG(*) FROM AccountGroupCounts WHERE ActiveMappingCount > 1),
        HistoricalPrivilegedP04Count = (SELECT COUNT_BIG(*) FROM ActiveMappings WHERE RoleClass = 1),
        HistoricalStandardP04Count = (SELECT COUNT_BIG(*) FROM ActiveMappings WHERE RoleClass = 2),
        OtherP04Count = (SELECT COUNT_BIG(*) FROM ActiveMappings WHERE RoleClass = 3),
        AccountReservedControlRows = (SELECT ReservedControlRows FROM AccountManifest),
        AccountManifestDigest = (SELECT ManifestDigest FROM AccountManifest),
        AllP04Mappings = (SELECT TotalMappings FROM MembershipManifest),
        MembershipReservedControlRows = (SELECT ReservedControlRows FROM MembershipManifest),
        MembershipManifestDigest = (SELECT ManifestDigest FROM MembershipManifest)
),
Evaluated AS
(
    SELECT m.*,
        ExactAccountManifestMatch = CASE
            WHEN TotalAccounts = 12
             AND AccountReservedControlRows = 0
             AND AccountManifestDigest = 'DFC216DE66DF33DC3681D9CAF03140EFC32793B715D0CDB98975AE60568534F9'
            THEN CONVERT(bigint, 1) ELSE CONVERT(bigint, 0) END,
        ExactMembershipManifestMatch = CASE
            WHEN AllP04Mappings = 12
             AND MembershipReservedControlRows = 0
             AND MembershipManifestDigest = '55FD16BD2AEACE351B7B1AEEE71ADDF5EFC890AA3C26EFD99FF0B09D2738478C'
            THEN CONVERT(bigint, 1) ELSE CONVERT(bigint, 0) END,
        AggregateHistoricalDemoSignatureMatch = CASE
            WHEN TotalAccounts = 12
             AND ActiveAccounts = 12
             AND LockedAccounts = 0
             AND InactiveAccounts = 0
             AND NullStatusFlagAccounts = 0
             AND DistinctVerifierCount = 1
             AND LargestVerifierCohort = 12
             AND NullOrEmptyVerifierAccounts = 0
             AND NullOrEmptyNormalizedLoginAccounts = 0
             AND NormalizedLoginDuplicateCohorts = 0
             AND AccountsInDuplicateLoginCohorts = 0
             AND MissingCompanyAccounts = 1
             AND MissingDepartmentAccounts = 1
             AND ActiveP04Mappings = 12
             AND DistinctP04MappedUsers = 12
             AND ActiveP04UserOrphans = 0
             AND ActiveP04GroupOrphans = 0
             AND ActiveP04LocationOrphans = 0
             AND ActiveP04DeletedGroupReferences = 0
             AND ActiveP04DeletedLocationReferences = 0
             AND AccountsWithZeroActiveP04 = 0
             AND AccountsWithMultipleActiveP04 = 0
             AND HistoricalPrivilegedP04Count = 6
             AND HistoricalStandardP04Count = 6
             AND OtherP04Count = 0
            THEN CONVERT(bigint, 1) ELSE CONVERT(bigint, 0)
        END
    FROM Metrics AS m
)
SELECT metrics.Metric, metrics.Value
FROM Evaluated AS e
CROSS APPLY (VALUES
    (N'accounts_total', e.TotalAccounts),
    (N'accounts_active_unlocked', e.ActiveAccounts),
    (N'accounts_locked', e.LockedAccounts),
    (N'accounts_inactive', e.InactiveAccounts),
    (N'accounts_null_status', e.NullStatusFlagAccounts),
    (N'verifier_distinct_values', e.DistinctVerifierCount),
    (N'verifier_largest_shared_cohort', e.LargestVerifierCohort),
    (N'accounts_blank_verifier', e.NullOrEmptyVerifierAccounts),
    (N'accounts_blank_login', e.NullOrEmptyNormalizedLoginAccounts),
    (N'duplicate_login_cohorts', e.NormalizedLoginDuplicateCohorts),
    (N'accounts_in_duplicate_login_cohorts', e.AccountsInDuplicateLoginCohorts),
    (N'accounts_missing_company', e.MissingCompanyAccounts),
    (N'accounts_missing_department', e.MissingDepartmentAccounts),
    (N'memberships_active', e.ActiveP04Mappings),
    (N'memberships_active_distinct_users', e.DistinctP04MappedUsers),
    (N'memberships_orphan_users', e.ActiveP04UserOrphans),
    (N'memberships_orphan_groups', e.ActiveP04GroupOrphans),
    (N'memberships_orphan_locations', e.ActiveP04LocationOrphans),
    (N'memberships_deleted_group_refs', e.ActiveP04DeletedGroupReferences),
    (N'memberships_deleted_location_refs', e.ActiveP04DeletedLocationReferences),
    (N'accounts_without_active_group', e.AccountsWithZeroActiveP04),
    (N'accounts_with_multiple_groups', e.AccountsWithMultipleActiveP04),
    (N'memberships_admin', e.HistoricalPrivilegedP04Count),
    (N'memberships_user', e.HistoricalStandardP04Count),
    (N'memberships_other_role', e.OtherP04Count),
    (N'account_manifest_reserved_control_rows', e.AccountReservedControlRows),
    (N'membership_manifest_reserved_control_rows', e.MembershipReservedControlRows),
    (N'exact_account_manifest_match', e.ExactAccountManifestMatch),
    (N'exact_membership_manifest_match', e.ExactMembershipManifestMatch),
    (N'aggregate_historical_demo_signature_match', e.AggregateHistoricalDemoSignatureMatch),
    (N'safe_to_classify_all_accounts_as_demo', CASE
        WHEN e.AggregateHistoricalDemoSignatureMatch = 1
         AND e.ExactAccountManifestMatch = 1
         AND e.ExactMembershipManifestMatch = 1
        THEN CONVERT(bigint, 1) ELSE CONVERT(bigint, 0) END)
) AS metrics(Metric, Value)
ORDER BY metrics.Metric;
SQL
)"

audit_output="$(
  printf '%s\n' "$audit_sql" |
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
    echo "The database audit returned an unexpected output shape." >&2
    exit 1
  fi
  metrics["${BASH_REMATCH[1]}"]="${BASH_REMATCH[2]}"
done <<< "$audit_output"

required_metrics=(
  accounts_total
  accounts_active_unlocked
  accounts_locked
  accounts_inactive
  verifier_distinct_values
  verifier_largest_shared_cohort
  memberships_active
  aggregate_historical_demo_signature_match
  exact_account_manifest_match
  exact_membership_manifest_match
  safe_to_classify_all_accounts_as_demo
)

for metric in "${required_metrics[@]}"; do
  if [[ ! -v "metrics[$metric]" ]]; then
    echo "The database audit omitted a required metric." >&2
    exit 1
  fi
done

for metric in $(printf '%s\n' "${!metrics[@]}" | sort); do
  printf '%s=%s\n' "$metric" "${metrics[$metric]}"
done
