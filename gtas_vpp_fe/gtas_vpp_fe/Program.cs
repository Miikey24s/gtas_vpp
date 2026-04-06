using gtas_vpp_fe.Components;
using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Helpers.DTOs.Share;
using gtas_vpp_fe.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Radzen;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddRadzenComponents();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<GlobalClass>();
#region Cookie
// 1. Thêm cấu hình hỗ trợ Cookie policy
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = context => true;
    options.MinimumSameSitePolicy = SameSiteMode.None;
});

// 2. Thêm Authentication với Cookie scheme
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = Config.CookieName;
        options.LoginPath = Config.LoginPagePath;
        options.AccessDeniedPath = Config.LoginPagePath;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(Config.CookieExpireMinutes);
    });

// 3. Cấu hình cho Blazor biết đang có Authentication
builder.Services.AddAuthorization();
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

var app = builder.Build();

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

app.Run();
