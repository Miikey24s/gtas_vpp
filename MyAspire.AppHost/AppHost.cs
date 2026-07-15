var builder = DistributedApplication.CreateBuilder(args);

var testDatabaseConnectionString = builder.AddParameter("test-database-connection-string", secret: true);
var jwtKey = builder.AddParameter("jwt-key", secret: true);
var passwordEncryptionKey = builder.AddParameter("password-encryption-key", secret: true);

var api = builder.AddProject<Projects.gtas_vpp_be>("backend")
    .WithEnvironment("DatabaseSettings__DefaultEnvironment", "TestEnv")
    .WithEnvironment("ConnectionStrings__TestEnv", testDatabaseConnectionString)
    .WithEnvironment("DatabaseInitialization__Mode", "MigrateAndReference")
    .WithEnvironment("DatabaseInitialization__Environments__0", "TestEnv")
    .WithEnvironment("JwtSettings__Key", jwtKey)
    .WithEnvironment("JwtSettings__Audience", "gtas_vpp_test_clients")
    .WithEnvironment("PasswordEncryption__Key", passwordEncryptionKey)
    .WithEnvironment("ReportInsights__Enabled", "false");

builder.AddProject<Projects.gtas_vpp_fe>("frontend")
    .WithReference(api)
    .WaitFor(api)
    .WithEnvironment("ApiSettings__BaseUrl", api.GetEndpoint("https"))
    .WithEnvironment("DOTNET_USE_SHARED_COMPILATION", "false")
    .WithEnvironment("MSBUILDDISABLENODEREUSE", "1");

builder.Build().Run();
