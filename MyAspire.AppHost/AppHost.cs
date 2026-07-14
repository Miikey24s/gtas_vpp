var builder = DistributedApplication.CreateBuilder(args);

var sqlPassword = builder.AddParameter("sql-password", secret: true);
var jwtKey = builder.AddParameter("jwt-key", secret: true);
var passwordEncryptionKey = builder.AddParameter("password-encryption-key", secret: true);

var database = builder.AddSqlServer("sql", password: sqlPassword)
    .WithDataVolume("gtas-vpp-aspire-sql")
    .AddDatabase("TestEnv", "GTAS_VPP_LIVE");

var api = builder.AddProject<Projects.gtas_vpp_be>("backend")
    .WithReference(database)
    .WaitFor(database)
    .WithEnvironment("DatabaseSettings__DefaultEnvironment", "TestEnv")
    .WithEnvironment("DatabaseInitialization__Mode", "MigrateAndSeed")
    .WithEnvironment("DatabaseInitialization__Environments__0", "TestEnv")
    .WithEnvironment("JwtSettings__Key", jwtKey)
    .WithEnvironment("PasswordEncryption__Key", passwordEncryptionKey)
    .WithEnvironment("ReportInsights__Enabled", "false");

builder.AddProject<Projects.gtas_vpp_fe>("frontend")
    .WithReference(api)
    .WaitFor(api)
    .WithEnvironment("ApiSettings__BaseUrl", api.GetEndpoint("https"))
    .WithEnvironment("DOTNET_USE_SHARED_COMPILATION", "false")
    .WithEnvironment("MSBUILDDISABLENODEREUSE", "1");

builder.Build().Run();
