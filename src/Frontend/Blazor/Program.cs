using gtas_vpp_fe.Platform.Composition;

var builder = WebApplication.CreateBuilder(args);
var settings = FrontendRuntimeSettings.Resolve(builder.Configuration, builder.Environment);

builder.Services
    .AddFrontendPlatform(settings)
    .AddFrontendFeatures(settings)
    .AddFrontendAuthenticationAndLocalization(settings);

var app = builder.Build();

app.UseFrontendPipeline(settings)
    .MapFrontendEndpoints();

app.Run();
