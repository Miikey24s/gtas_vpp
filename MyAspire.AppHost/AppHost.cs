var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.gtas_vpp_be>("backend");
builder.AddProject<Projects.gtas_vpp_fe>("frontend")
    .WithReference(api)
    .WithEnvironment("ApiSettings__BaseUrl", api.GetEndpoint("https"))
    .WithEnvironment("DOTNET_USE_SHARED_COMPILATION", "false")
    .WithEnvironment("MSBUILDDISABLENODEREUSE", "1");
builder.Build().Run();
