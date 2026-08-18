using gtas_vpp_fe.Features.CatalogPricing.Api;
using gtas_vpp_fe.Features.IdentityAccess.Api;
using gtas_vpp_fe.Features.IdentityAccess.State;
using gtas_vpp_fe.Features.Notifications.Api;
using gtas_vpp_fe.Features.Notifications.Realtime;
using gtas_vpp_fe.Features.Notifications.State;
using gtas_vpp_fe.Features.Reports.Api;
using gtas_vpp_fe.Features.Requests.Api;
using gtas_vpp_fe.Features.Requests.Drafts;
using gtas_vpp_fe.Features.Requests.Submission;
using gtas_vpp_fe.Features.Settlement.Api;
using gtas_vpp_fe.Features.Settlement.State;
using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Platform.Browser;
using gtas_vpp_fe.Platform.Auth;
using gtas_vpp_fe.Platform.State;
using gtas_vpp_fe.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Radzen;
using System.Net.Security;

namespace gtas_vpp_fe.Platform.Composition;

public static class FrontendServiceCollectionExtensions
{
    public static IServiceCollection AddFrontendPlatform(
        this IServiceCollection services,
        FrontendRuntimeSettings settings)
    {
        Directory.CreateDirectory(settings.DataProtectionKeysPath);

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(settings.DataProtectionKeysPath))
            .SetApplicationName("gtas_vpp");
        services.AddHealthChecks();
        services.AddRazorComponents()
            .AddInteractiveServerComponents()
            .AddHubOptions(options =>
            {
                // Ứng dụng không upload payload lớn; giới hạn 64 KB giảm bề mặt DoS cho circuit.
                options.MaximumReceiveMessageSize = 64 * 1024;
                options.KeepAliveInterval = TimeSpan.FromSeconds(15);
                options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
                options.HandshakeTimeout = TimeSpan.FromSeconds(30);
            });
        services.AddRadzenComponents();
        services.AddHttpContextAccessor();

        services.AddScoped<UiBusyState>();
        services.AddScoped<ThemeState>();
        services.AddScoped<IToastService, ToastService>();
        services.AddScoped<IBrowserFileDownloadService, BrowserFileDownloadService>();

        services.AddHttpClient(Config.HttpClientName, client => ConfigureApiClient(client, settings))
            .ConfigurePrimaryHttpMessageHandler(() => CreateApiHttpHandler(settings));
        services.AddHttpClient<IAPIServices, APIServices>(client => ConfigureApiClient(client, settings))
            .ConfigurePrimaryHttpMessageHandler(() => CreateApiHttpHandler(settings));

        return services;
    }

    public static IServiceCollection AddFrontendFeatures(
        this IServiceCollection services,
        FrontendRuntimeSettings settings)
    {
        services.AddScoped<LookupApiClient>();
        services.AddScoped<CatalogApiClient>();
        services.AddScoped<PricingApiClient>();
        services.AddScoped<PriceListImportApiClient>();

        services.AddScoped<AuthHelper>();
        services.AddScoped<CurrentUserState>();
        services.AddScoped<PermissionAdministrationApiClient>();
        services.AddScoped<UserAdministrationApiClient>();
        services.AddScoped<IAuthSessionInvalidationCoordinator, AuthSessionInvalidationCoordinator>();
        services.AddScoped<PermissionRefreshSignal>();
        services.AddScoped<PermissionState>();
        services.AddScoped<PermissionRealtimeService>();
        services.AddSingleton<LoginTicketCache>();
        services.AddHttpClient<AccountApiClient>(client => ConfigureApiClient(client, settings))
            .ConfigurePrimaryHttpMessageHandler(() => CreateApiHttpHandler(settings));
        services.AddHttpClient<AuthenticationApiClient>(client => ConfigureApiClient(client, settings))
            .ConfigurePrimaryHttpMessageHandler(() => CreateApiHttpHandler(settings));

        services.AddScoped<NotificationApiClient>();
        services.AddScoped<INotificationRealtimeClient, NotificationRealtimeClient>();
        services.AddScoped<NotificationInboxState>();

        services.AddScoped<RequestsQueryClient>();
        services.AddScoped<RequestsCommandClient>();
        services.AddScoped<RequestsExportClient>();
        services.AddScoped<OrderPeriodApiClient>();
        services.AddScoped<OrderDraftStore>();
        services.AddScoped<OrderSubmissionCoordinator>();

        services.AddScoped<SettlementApiClient>();
        services.AddScoped<PostSettlementOrderCorrectionApiClient>();
        services.AddScoped<PeriodSettlementState>();
        services.AddSingleton(new SettlementFeatureOptions(settings.EnableMultiSupplierSettlement));
        services.AddScoped<ReportsApiClient>();

        return services;
    }

    public static IServiceCollection AddFrontendAuthenticationAndLocalization(
        this IServiceCollection services,
        FrontendRuntimeSettings settings)
    {
        services.AddLocalization(options => options.ResourcesPath = "Resources");
        services.Configure<CookiePolicyOptions>(options =>
        {
            options.CheckConsentNeeded = _ => true;
            options.MinimumSameSitePolicy = SameSiteMode.Lax;
        });
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = Config.CookieName;
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = settings.AuthCookieSecurePolicy;
                options.LoginPath = Config.LoginPagePath;
                options.AccessDeniedPath = Config.LoginPagePath;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(Config.CookieExpireMinutes);
                options.SlidingExpiration = false;
            });
        services.AddAuthorization();
        services.AddCascadingAuthenticationState();

        return services;
    }

    private static void ConfigureApiClient(HttpClient client, FrontendRuntimeSettings settings)
    {
        client.BaseAddress = settings.ApiBaseUri;
        client.Timeout = settings.ApiRequestTimeout;
    }

    private static SocketsHttpHandler CreateApiHttpHandler(FrontendRuntimeSettings settings)
    {
        var handler = new SocketsHttpHandler
        {
            ConnectTimeout = settings.ApiConnectTimeout,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5)
        };

        if (settings.BypassApiServerCertificateValidation)
        {
            handler.SslOptions = new SslClientAuthenticationOptions
            {
                RemoteCertificateValidationCallback = static (_, _, _, _) => true
            };
        }

        return handler;
    }
}
