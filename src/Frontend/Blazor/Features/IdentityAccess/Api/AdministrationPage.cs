namespace gtas_vpp_fe.Features.IdentityAccess.Api;

public sealed record AdministrationPage<T>(
    IReadOnlyList<T> Items,
    int TotalCount);
