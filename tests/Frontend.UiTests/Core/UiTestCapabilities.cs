namespace gtas_vpp_fe.UITests.Core;

/// <summary>Marks a browser test that needs a harness-owned authenticated QA fixture.</summary>
public interface IAuthenticatedUiTest;

/// <summary>Marks a browser test that can change server/database state.</summary>
public interface IMutatingUiTest : IAuthenticatedUiTest;
