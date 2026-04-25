using gtas_vpp_fe.Components;
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
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseCookiePolicy();
app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapGet("/perform-login", async (
    string id,
    string? returnUrl,
    gtas_vpp_fe.Helpers.LoginTicketCache cache,
    HttpContext context) =>
{
    var data = cache.Get(id);
    if (data == null)
    {
        return Microsoft.AspNetCore.Http.Results.Redirect("/Account/Login");
    }

    var (loginData, server, rememberMe) = data.Value;

    var claims = loginData.sp_AuthenticationLogin_To_Claims();
    claims.Add(new System.Security.Claims.Claim(ClaimKeys.Server, server));

    var claimsIdentity = new System.Security.Claims.ClaimsIdentity(claims, Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
    var claimsPrincipal = new System.Security.Claims.ClaimsPrincipal(claimsIdentity);

    var authProperties = new Microsoft.AspNetCore.Authentication.AuthenticationProperties
    {
        IsPersistent = rememberMe,
        AllowRefresh = true,
    };

    if (rememberMe)
    {
        authProperties.ExpiresUtc = DateTimeOffset.UtcNow.AddHours(gtas_vpp_fe.Helpers.Config.AuthPropertyExpireHours);
    }

    await Microsoft.AspNetCore.Authentication.AuthenticationHttpContextExtensions.SignInAsync(
        context,
        Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme,
        claimsPrincipal,
        authProperties);

    if (!string.IsNullOrWhiteSpace(returnUrl))
    {
        return Microsoft.AspNetCore.Http.Results.Redirect(returnUrl);
    }
    return Microsoft.AspNetCore.Http.Results.Redirect("/");
});

app.Run();

