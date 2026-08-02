using gtas_vpp_fe.Components;
using gtas_vpp_fe.Endpoints;
using gtas_vpp_fe.Features.CatalogPricing.Api;
using gtas_vpp_fe.Features.IdentityAccess.Api;
using gtas_vpp_fe.Features.Reports.Api;
using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Platform.State;
using gtas_vpp_fe.Services;
using gtas_vpp_fe.State;
using gtas_vpp_shared.Constants;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using System.Net.Security;
using Radzen;

var builder = WebApplication.CreateBuilder(args);
var authCookieSecurePolicy = builder.Environment.IsDevelopment()
    ? CookieSecurePolicy.SameAsRequest
    : CookieSecurePolicy.Always;
var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"];
if (string.IsNullOrWhiteSpace(dataProtectionKeysPath))
{
    dataProtectionKeysPath = builder.Environment.IsProduction()
        ? "/app/keys"
        : Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GTAS_VPP",
            "DataProtection-Keys");
}
Directory.CreateDirectory(dataProtectionKeysPath);

// ── 1. Service xử lý Forwarded Headers (phải đăng ký trước) ──
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Cấu hình Data Protection cho Docker để cookie không mất hiệu lực sau khi restart.
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
    .SetApplicationName("gtas_vpp");
builder.Services.AddHealthChecks();

// Đăng ký service vào DI container.
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddHubOptions(options =>
    {
        // Giữ một profile production tường minh cho circuit Interactive Server global.
        // Ứng dụng không có workflow upload lớn từ client lên server, nên giới hạn message
        // 64 KB giúp loại bề mặt DoS 50 MB trước đây.
        options.MaximumReceiveMessageSize = 64 * 1024;
        options.KeepAliveInterval = TimeSpan.FromSeconds(15);
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
        options.HandshakeTimeout = TimeSpan.FromSeconds(30);
    });
builder.Services.AddRadzenComponents();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<GlobalClass>();
builder.Services.AddScoped<UiBusyState>();
builder.Services.AddScoped<ThemeState>();
builder.Services.AddScoped<AuthHelper>();
builder.Services.AddScoped<CurrentUserState>();
builder.Services.AddScoped<LookupApiClient>();
builder.Services.AddScoped<IAuthSessionInvalidationCoordinator, AuthSessionInvalidationCoordinator>();
builder.Services.AddScoped<PermissionRefreshSignal>();
builder.Services.AddScoped<PermissionState>();
builder.Services.AddScoped<PermissionRealtimeService>();
builder.Services.AddScoped<NotificationInboxState>();
builder.Services.AddScoped<gtas_vpp_fe.Components.Pages.VPPRequest.Components.PeriodSettlementState>();
builder.Services.AddScoped<IToastService, ToastService>();
builder.Services.AddScoped<IBrowserFileDownloadService, BrowserFileDownloadService>();
builder.Services.AddScoped<ReportsApiClient>();
builder.Services.AddSingleton<LoginTicketCache>();
#region Cookie
// Cấu hình hỗ trợ Cookie policy
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = context => true;
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
});

// Authentication với Cookie scheme
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = Config.CookieName;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = authCookieSecurePolicy;
        options.LoginPath = Config.LoginPagePath;
        options.AccessDeniedPath = Config.LoginPagePath;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(Config.CookieExpireMinutes);
        options.SlidingExpiration = false;
    });

// Cấu hình cho Blazor biết đang có Authentication
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
#endregion
#region API
var apiBaseUri = ApiBaseUrlResolver.Resolve(builder.Configuration["ApiSettings:BaseUrl"]);
var apiConnectTimeout = TimeSpan.FromSeconds(
    builder.Configuration.GetValue("ApiSettings:ConnectTimeoutSeconds", 5));
var apiRequestTimeout = TimeSpan.FromSeconds(
    builder.Configuration.GetValue("ApiSettings:RequestTimeoutSeconds", 30));

SocketsHttpHandler CreateApiHttpHandler()
{
    var handler = new SocketsHttpHandler
    {
        ConnectTimeout = apiConnectTimeout,
        PooledConnectionLifetime = TimeSpan.FromMinutes(5)
    };

    if (ApiBaseUrlResolver.ShouldBypassServerCertificateValidation(builder.Environment.IsDevelopment(), apiBaseUri))
    {
        handler.SslOptions = new SslClientAuthenticationOptions
        {
            RemoteCertificateValidationCallback = static (_, _, _, _) => true
        };
    }

    return handler;
}

void ConfigureApiClient(HttpClient client)
{
    client.BaseAddress = apiBaseUri;
    client.Timeout = apiRequestTimeout;
}

builder.Services.AddHttpClient(Config.HttpClientName, ConfigureApiClient)
.ConfigurePrimaryHttpMessageHandler(CreateApiHttpHandler);
builder.Services.AddHttpClient<IAPIServices, APIServices>(ConfigureApiClient)
.ConfigurePrimaryHttpMessageHandler(CreateApiHttpHandler);
builder.Services.AddHttpClient<AccountApiClient>(ConfigureApiClient)
.ConfigurePrimaryHttpMessageHandler(CreateApiHttpHandler);
builder.Services.AddHttpClient<AuthenticationApiClient>(ConfigureApiClient)
.ConfigurePrimaryHttpMessageHandler(CreateApiHttpHandler);
#endregion


var app = builder.Build();///////////////////////////////
app.Logger.LogInformation("Configured API base URL: {ApiBaseUrl}", apiBaseUri);
if (ApiBaseUrlResolver.ShouldBypassServerCertificateValidation(app.Environment.IsDevelopment(), apiBaseUri))
{
    app.Logger.LogWarning(
        "Ignoring HTTPS certificate validation for local development API endpoint {ApiBaseUrl}.",
        apiBaseUri);
}

// ══════════════════════════════════════════════════════════
// MIDDLEWARE PIPELINE - THỨ TỰ RẤT QUAN TRỌNG!
// ══════════════════════════════════════════════════════════

// ── PHẢI ĐẶT ĐẦU TIÊN: xử lý Forwarded Headers từ Nginx ──
// Blazor SignalR cần biết scheme thật (https) để tạo wss:// URL
app.UseForwardedHeaders();

var supportedCultures = new[] { new CultureInfo("vi"), new CultureInfo("en") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("vi"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

// Cấu hình HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // BỎ UseHsts() - Nginx đã xử lý HSTS
    // BỎ UseHttpsRedirection() - Nginx đã xử lý SSL termination
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

// ── STATIC FILES TRƯỚC AUTH - để _framework/blazor.web.js không bị auth chặn ──
app.UseStaticFiles();
app.MapStaticAssets();

app.UseCookiePolicy();
app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapLoginEndpoints();
app.MapHealthChecks("/health").AllowAnonymous();

if (app.Environment.IsDevelopment())
{
    app.MapGet("/debug/endpoints", (IEnumerable<Microsoft.AspNetCore.Routing.EndpointDataSource> endpointSources) =>
    {
        var endpoints = endpointSources.SelectMany(es => es.Endpoints).Select(e => new
        {
            DisplayName = e.DisplayName,
            RoutePattern = (e as Microsoft.AspNetCore.Routing.RouteEndpoint)?.RoutePattern.RawText
        });
        return endpoints;
    });
}

app.Run();
