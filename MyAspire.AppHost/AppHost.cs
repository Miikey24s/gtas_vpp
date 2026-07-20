var builder = DistributedApplication.CreateBuilder(args);

var testDatabaseConnectionString = builder.AddParameter("test-database-connection-string", secret: true);
var jwtKey = builder.AddParameter("jwt-key", secret: true);

var api = builder.AddProject<Projects.gtas_vpp_be>("backend")
    .WithEnvironment("DatabaseSettings__DefaultEnvironment", "TestEnv")
    .WithEnvironment("ConnectionStrings__TestEnv", testDatabaseConnectionString)
    .WithEnvironment("DatabaseInitialization__Mode", "MigrateAndReference")
    .WithEnvironment("DatabaseInitialization__Environments__0", "TestEnv")
    .WithEnvironment("JwtSettings__Key", jwtKey)
    .WithEnvironment("JwtSettings__Audience", "gtas_vpp_test_clients");

if (!string.IsNullOrWhiteSpace(builder.Configuration["Parameters:qa-fixture-run-id"]))
{
    var qaFixtureRunId = builder.AddParameter("qa-fixture-run-id");
    api.WithEnvironment("ASPNETCORE_ENVIRONMENT", "Testing")
        .WithEnvironment("QaFixture__Enabled", "true")
        .WithEnvironment("QaFixture__RunId", qaFixtureRunId);
}

var frontend = builder.AddProject<Projects.gtas_vpp_fe>("frontend")
    .WithReference(api)
    .WaitFor(api)
    .WithEnvironment("ApiSettings__BaseUrl", api.GetEndpoint("https"))
    .WithEnvironment("DOTNET_USE_SHARED_COMPILATION", "false")
    .WithEnvironment("MSBUILDDISABLENODEREUSE", "1");

frontend.WithUrlForEndpoint("https", url =>
{
    url.DisplayText = "GTAS Login";
    url.Url = "/Account/Login";
});

var reactFrontend = builder.AddViteApp("frontend-react", "../gtas_vpp_fe_react")
    .WithReference(api)
    .WaitFor(api)
    .WithEnvironment("GTAS_API_PROXY_TARGET", api.GetEndpoint("https"))
    .WithEnvironment("VITE_NOTIFICATIONS_REALTIME", "true")
    .WithExternalHttpEndpoints();

reactFrontend.WithUrlForEndpoint("http", url =>
{
    url.DisplayText = "GTAS React Preview";
});

builder.Build().Run();
