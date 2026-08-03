namespace gtas_vpp_test_support;

public static class QaTestData
{
    public const long CompanyCode = 77500;
    public const string CompanyName = "GTAS QA Company";
    public const string DepartmentAlphaCode = "QA-D01";
    public const string DepartmentBetaCode = "QA-D02";

    public static readonly Guid DepartmentAlphaId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    public static readonly Guid DepartmentBetaId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    public static readonly Guid CurrentPeriodId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    public static readonly Guid PreviousSettlementPeriodId = Guid.Parse("20000000-0000-0000-0000-000000000002");
    public static readonly Guid OwnRequestId = Guid.Parse("40000000-0000-0000-0000-000000000001");
    public static readonly Guid DepartmentPeerRequestId = Guid.Parse("40000000-0000-0000-0000-000000000002");
    public static readonly Guid CompanyOtherDepartmentRequestId = Guid.Parse("40000000-0000-0000-0000-000000000003");
    public static readonly Guid SettlementRequestId = Guid.Parse("40000000-0000-0000-0000-000000000004");
    public static readonly Guid SettlementRequestDetailId = Guid.Parse("50000000-0000-0000-0000-000000000004");
}

public sealed class QaTestAccount
{
    public QaTestAccount(
        int userId,
        string username,
        string fullName,
        string departmentCode,
        Guid departmentId,
        string role,
        string password)
    {
        UserId = userId;
        Username = username;
        FullName = fullName;
        DepartmentCode = departmentCode;
        DepartmentId = departmentId;
        Role = role;
        Password = password;
    }

    public int UserId { get; }

    public string Username { get; }

    public string FullName { get; }

    public string DepartmentCode { get; }

    public Guid DepartmentId { get; }

    public string Role { get; }

    public string Password { get; }

    public string Email => $"{Username}@example.invalid";

    public override string ToString() => $"{Role}:{Username} (password redacted)";
}

public sealed class QaTestAccounts
{
    private QaTestAccounts(QaFixtureSecrets secrets)
    {
        Employee = Create(1_000_001_001, "qa_employee", "QA Employee", QaTestData.DepartmentAlphaCode,
            QaTestData.DepartmentAlphaId, "Employee", secrets);
        DepartmentPeer = Create(1_000_001_002, "qa_employee_peer", "QA Department Peer", QaTestData.DepartmentAlphaCode,
            QaTestData.DepartmentAlphaId, "Employee", secrets);
        OtherDepartmentEmployee = Create(1_000_001_003, "qa_employee_other", "QA Other Department", QaTestData.DepartmentBetaCode,
            QaTestData.DepartmentBetaId, "Employee", secrets);
        Manager = Create(1_000_001_004, "qa_manager", "QA Department Approver", QaTestData.DepartmentAlphaCode,
            QaTestData.DepartmentAlphaId, "DepartmentApprover", secrets);
        Procurement = Create(1_000_001_005, "qa_procurement", "QA Procurement", QaTestData.DepartmentAlphaCode,
            QaTestData.DepartmentAlphaId, "Procurement", secrets);
        SystemAdmin = Create(1_000_001_006, "qa_sysadmin", "QA System Admin", QaTestData.DepartmentAlphaCode,
            QaTestData.DepartmentAlphaId, "SystemAdmin", secrets);

        All =
        [
            Employee,
            DepartmentPeer,
            OtherDepartmentEmployee,
            Manager,
            Procurement,
            SystemAdmin
        ];
    }

    public QaTestAccount Employee { get; }

    public QaTestAccount DepartmentPeer { get; }

    public QaTestAccount OtherDepartmentEmployee { get; }

    public QaTestAccount Manager { get; }

    public QaTestAccount Procurement { get; }

    public QaTestAccount SystemAdmin { get; }

    public IReadOnlyList<QaTestAccount> All { get; }

    public static QaTestAccounts Create(QaFixtureSecrets secrets) => new(secrets);

    private static QaTestAccount Create(
        int userId,
        string username,
        string fullName,
        string departmentCode,
        Guid departmentId,
        string role,
        QaFixtureSecrets secrets)
    {
        return new QaTestAccount(
            userId,
            username,
            fullName,
            departmentCode,
            departmentId,
            role,
            secrets.AccountPassword);
    }
}
