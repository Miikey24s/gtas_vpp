using gtas_vpp_fe.Components;
using gtas_vpp_fe.Endpoints;
using gtas_vpp_fe.Helpers;
using gtas_vpp_shared.DTOs.Share;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.Constants;
using Microsoft.AspNetCore.Authentication.Cookies;
using Radzen;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddRadzenComponents();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<GlobalClass>();
builder.Services.AddScoped<AuthHelper>();
builder.Services.AddScoped<ICustomNotificationService, CustomNotificationService>();
builder.Services.AddSingleton<LoginTicketCache>();
#region Cookie
// 1. ThÃªm cáº¥u hÃ¬nh há»— trá»£ Cookie policy
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = context => true;
    options.MinimumSameSitePolicy = SameSiteMode.None;
});

// 2. ThÃªm Authentication vá»›i Cookie scheme
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = Config.CookieName;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.None;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.LoginPath = Config.LoginPagePath;
        options.AccessDeniedPath = Config.LoginPagePath;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(Config.CookieExpireMinutes);
    });

// 3. Cáº¥u hÃ¬nh cho Blazor biáº¿t Ä‘ang cÃ³ Authentication
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
#region SeriLog
//Log.Logger = new LoggerConfiguration()
//    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Fatal) // bá» log cá»§a ASP.NET
//    .MinimumLevel.Debug() // chá»‰ nháº­n log do mÃ¬nh ghi
//    .Enrich.FromLogContext()
//    .WriteTo.Console()
//    .WriteTo.File(
//        path: Path.Combine("wwwroot", "logs", "log-.txt"),
//        rollingInterval: RollingInterval.Day,
//        retainedFileCountLimit: 10,
//        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
//        shared: true
//    )
//    .CreateLogger();
//builder.Host.UseSerilog();
#endregion


var app = builder.Build();///////////////////////////////

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Behind nginx reverse proxy: trust forwarded headers (X-Forwarded-Proto = https)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
                     | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
});

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

// Only redirect to HTTPS in development; in production nginx handles SSL termination
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCookiePolicy();
app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapLoginEndpoints();

app.Run();

