using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using DesignDnaStudio.Web.Components;
using DesignDnaStudio.Web.Data;
using DesignDnaStudio.Web.Infrastructure;
using DesignDnaStudio.Web.Services.Assessment;
using DesignDnaStudio.Engine;
using Microsoft.Data.Sqlite;
using Radzen;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddLocalization();
builder.Services.AddRadzenComponents();

var studioPaths = StudioDataPaths.Resolve(builder.Configuration, builder.Environment.ContentRootPath);
builder.Services.AddSingleton(studioPaths);

var connectionString = new SqliteConnectionStringBuilder
{
    DataSource = studioPaths.DatabasePath,
    Cache = SqliteCacheMode.Shared,
    ForeignKeys = true
}.ToString();
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<PreferenceLearner>();
builder.Services.AddSingleton<AdaptivePairSelector>();
builder.Services.AddScoped<IDesignProfileNarrativeService, DeterministicDesignProfileNarrativeService>();
builder.Services.AddScoped<AssessmentService>();
builder.Services.AddScoped<StudioDatabaseInitializer>();

var app = builder.Build();
app.Use(async (context, next) =>
{
    var host = context.Request.Host.Host;
    var hostIsLoopback = string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
                         IPAddress.TryParse(host, out var hostAddress) && IPAddress.IsLoopback(hostAddress);
    var remoteIsLoopback = context.Connection.RemoteIpAddress is null ||
                           IPAddress.IsLoopback(context.Connection.RemoteIpAddress);

    if (hostIsLoopback && remoteIsLoopback)
    {
        await next();
        return;
    }

    context.Response.StatusCode = StatusCodes.Status403Forbidden;
    await context.Response.WriteAsync("DesignDNA Studio chỉ chấp nhận kết nối từ máy local.");
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapGet("/studio/export/{sessionId:guid}", async (
    Guid sessionId,
    HttpContext context,
    AssessmentService assessmentService,
    CancellationToken cancellationToken) =>
{
    var export = await assessmentService.CreateExportAsync(sessionId, cancellationToken);
    context.Response.Headers.CacheControl = "no-store";
    context.Response.Headers.Pragma = "no-cache";
    return export is null
        ? Results.NotFound()
        : Results.File(
            JsonSerializer.SerializeToUtf8Bytes(export, StudioJson.Options),
            "application/json; charset=utf-8",
            $"personal-design-dna-{sessionId:N}.json");
});

if (!builder.Configuration.GetValue<bool>("Studio:SkipStartupInitialization"))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<StudioDatabaseInitializer>().InitializeAsync();
}

app.Run();
