using gtas_vpp_fe.Components;
using gtas_vpp_fe.Endpoints;
using gtas_vpp_fe.Helpers;
using gtas_vpp_shared.DTOs.Share;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.Constants;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.DataProtection;
using Radzen;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ── 1. Forwarded Headers Service (phải đăng ký trước) ────
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Configure Data Protection for Docker so cookies don't get invalidated on restart
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(@"/app/keys"))
    .SetApplicationName("gtas_vpp");

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddHubOptions(options =>
    {
        // Tăng giới hạn payload của SignalR lên 50MB để tránh lỗi khi gửi/nhận dữ liệu lớn
        options.MaximumReceiveMessageSize = 50 * 1024 * 1024;
    });
builder.Services.AddRadzenComponents();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<GlobalClass>();
builder.Services.AddScoped<AuthHelper>();
builder.Services.AddScoped<ICustomNotificationService, CustomNotificationService>();
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
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.LoginPath = Config.LoginPagePath;
        options.AccessDeniedPath = Config.LoginPagePath;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(Config.CookieExpireMinutes);
    });

// Cấu hình cho Blazor biết đang có Authentication
builder.Services.AddAuthorization(options =>
{
    foreach (var permission in Permissions.All)
    {
        options.AddPolicy(permission, policy => policy.RequireClaim(ClaimKeys.Permission, permission));
    }
});
builder.Services.AddCascadingAuthenticationState();
#endregion
#region API
builder.Services.AddHttpClient(Config.HttpClientName, client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseUrl"]!);
});
builder.Services.AddHttpClient<IAPIServices, APIServices>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseUrl"]!);
});
#endregion


var app = builder.Build();///////////////////////////////

// ══════════════════════════════════════════════════════════
// MIDDLEWARE PIPELINE - THỨ TỰ RẤT QUAN TRỌNG!
// ══════════════════════════════════════════════════════════

// ── PHẢI ĐẶT ĐẦU TIÊN: Forwarded Headers từ Nginx ──────
// Blazor SignalR cần biết scheme thật (https) để tạo wss:// URL
app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
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

app.MapGet("/debug/endpoints", (IEnumerable<Microsoft.AspNetCore.Routing.EndpointDataSource> endpointSources) =>
{
    var endpoints = endpointSources.SelectMany(es => es.Endpoints).Select(e => new 
    {
        DisplayName = e.DisplayName,
        RoutePattern = (e as Microsoft.AspNetCore.Routing.RouteEndpoint)?.RoutePattern.RawText
    });
    return endpoints;
});

app.Run();
