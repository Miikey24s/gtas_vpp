using gtas_vpp_be.Authorization;
using gtas_vpp_be.Mappings;
using gtas_vpp_be.Middleware;
using gtas_vpp_be.Notifications;
using gtas_vpp_be.Model;
using gtas_vpp_be.Model.Auth;
using gtas_vpp_be.Service.Domain;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Helpers.Context;
using gtas_vpp_be.Service.Services;
using gtas_vpp_be.Service.Services.AuthBootstrap;
using gtas_vpp_shared.Constants;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Serilog;
using System.Text;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading.RateLimiting;
using System.Security.Claims;
using System.Globalization;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);
var Configuration = builder.Configuration;
var accountTokenKeysPath = Configuration["DataProtection:KeysPath"];
if (string.IsNullOrWhiteSpace(accountTokenKeysPath))
{
    accountTokenKeysPath = builder.Environment.IsProduction()
        ? "/app/keys"
        : Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GTAS_VPP",
            "Backend-DataProtection-Keys");
}
Directory.CreateDirectory(accountTokenKeysPath);
var databaseBinding = DatabaseBinding.Create(Configuration);
DeploymentConfigurationContract.ValidateDatabaseBindingForHost(
    builder.Environment.EnvironmentName,
    builder.Environment.IsProduction(),
    databaseBinding);
var qaFixtureIdentity = QaFixtureIdentityContract.Resolve(
    builder.Environment,
    Configuration,
    databaseBinding);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 50 * 1024 * 1024; // 50MB
});

var jwtSettings = JwtDeploymentSettings.Create(Configuration, databaseBinding);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/log-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14)
    .CreateLogger();
builder.Host.UseSerilog();

// Đăng ký service vào DI container.

builder.Services.AddControllers();
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(accountTokenKeysPath))
    .SetApplicationName("gtas_vpp_backend_identity");
// Xem thêm cách cấu hình OpenAPI tại https://aka.ms/aspnet/openapi

builder.Services.AddSingleton(databaseBinding);
builder.Services.AddSingleton(jwtSettings);
builder.Services.AddDbContext<VPPMigrationDbContext>(
    (sp, o) =>
    {
        o.UseSqlServer(
            databaseBinding.ConnectionString,
            action => action.MigrationsAssembly(Config.DatabaseSettings.MigrationsAssembly));
    }
);
builder.Services.AddDbContext<VPPContext>(
    (sp, o) =>
    {
        o.UseSqlServer(databaseBinding.ConnectionString);
    }
);

builder.Services.AddHttpContextAccessor();
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[]
    {
        new CultureInfo("vi-VN"),
        new CultureInfo("en-US")
    };
    options.DefaultRequestCulture = new RequestCulture("vi-VN");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.ApplyCurrentCultureToResponseHeaders = true;
});
builder.Services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
builder.Services.AddSingleton(sp => VppRequestPolicy.FromConfiguration(
    sp.GetRequiredService<IConfiguration>()));
builder.Services.AddSingleton(sp => new PeriodCalculator(
    sp.GetRequiredService<VppRequestPolicy>().DeadlineDay));
builder.Services.AddSingleton<PeriodScheduleCalculator>();
builder.Services.AddScoped<IUserNameResolver, UserNameResolver>();
builder.Services.AddScoped<IDynamicDbContextFactory, DynamicDbContextFactory>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IVPPRequestService, VPPRequestService>();
builder.Services.AddScoped<IVppDashboardChartQueryService, VppDashboardChartQueryService>();
builder.Services.AddScoped<IVppPeriodService, VppPeriodService>();
builder.Services.AddHostedService<VppPeriodRecoveryWorker>();
builder.Services.AddScoped<IVPPPriceService, VPPPriceService>();
builder.Services.AddScoped<IPriceAsOfResolver, PriceAsOfResolver>();
builder.Services.AddScoped<IPriceBookWorkflowService, PriceBookWorkflowService>();
builder.Services.AddScoped<IPriceListService, PriceListService>();
builder.Services.AddScoped<IPriceListExportService, PriceListExportService>();
builder.Services.AddSingleton<PriceListImportFileParser>();
builder.Services.AddPriceListColumnMapping(Configuration);
builder.Services.AddScoped<IPriceListImportService, PriceListImportService>();
builder.Services.AddScoped<IPeriodSettlementService, PeriodSettlementService>();
builder.Services.AddScoped<IPostSettlementOrderCorrectionService, PostSettlementOrderCorrectionService>();
builder.Services.AddScoped<IVppCatalogService, VppCatalogService>();
builder.Services.AddScoped<ILibraryIntegrityService, LibraryIntegrityService>();
builder.Services.AddScoped<ILibraryQueryService, LibraryQueryService>();
builder.Services.AddReportsModule(Configuration);
builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.CustomSchemaIds(type =>
    {
        var isAuthResponseNamespace = string.Equals(
            type.Namespace,
            "gtas_vpp_shared.DTOs.Res.Auth",
            StringComparison.Ordinal);

        return isAuthResponseNamespace
            && type.Name is "PermissionGroupResDTO" or "UserGroupMembershipResDTO"
                ? $"Auth{type.Name}"
                : type.Name;
    });
});
builder.Services.AddHealthChecks()
    .AddAsyncCheck(
        "database",
        async cancellationToken =>
        {
            try
            {
                await using var connection = new SqlConnection(databaseBinding.ConnectionString);
                await connection.OpenAsync(cancellationToken);
                return HealthCheckResult.Healthy();
            }
            catch (Exception exception)
            {
                return HealthCheckResult.Unhealthy("Database connection failed.", exception);
            }
        },
        timeout: TimeSpan.FromSeconds(5));
builder.Services.AddSignalR();
builder.Services
    .AddIdentityCore<AppUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 10;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<VPPContext>()
    .AddDefaultTokenProviders();
builder.Services.AddScoped<IAccountLifecycleService, AccountLifecycleService>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                // Chỉ nới khi chạy QA isolated (qaFixtureIdentity != null: QaFixture:Enabled
                // + môi trường Testing + LocalDB QA, fail-closed). Suite E2E dùng chung một
                // app nên mọi lần đăng nhập đến từ cùng một IP loopback; giữ 5/phút sẽ trả
                // 429 cho các test hợp lệ. Môi trường thật vẫn giữ nguyên 5/phút.
                PermitLimit = qaFixtureIdentity is not null ? 1000 : 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    options.AddPolicy("report-insights", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.FindFirst("UserID")?.Value
                ?? httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    options.AddPolicy("account-register", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    options.AddPolicy("account-recovery", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    options.AddPolicy("account-confirm", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    options.AddPolicy("account-password", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"{httpContext.User.FindFirst("UserID")?.Value ?? "anonymous"}:" +
                          (httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

builder.Services.AddResponseCompression(opts =>
{
    opts.EnableForHttps = true;
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "application/json" });
});

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = AppAuthenticationSchemes.AntiforgeryHeaderName;
    options.Cookie.Name = "gtas-vpp-antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = AppAuthenticationSchemes.Default;
        options.DefaultChallengeScheme = AppAuthenticationSchemes.Default;
    })
    .AddPolicyScheme(
        AppAuthenticationSchemes.Default,
        displayName: null,
        options =>
        {
            options.ForwardDefaultSelector = context =>
                context.Request.Headers.Authorization.ToString()
                    .StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                    ? JwtBearerDefaults.AuthenticationScheme
                    : AppAuthenticationSchemes.Cookie;
        })
    .AddCookie(AppAuthenticationSchemes.Cookie, options =>
    {
        options.Cookie.Name = AppAuthenticationSchemes.SessionCookieName;
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsProduction()
            ? CookieSecurePolicy.Always
            : CookieSecurePolicy.SameAsRequest;
        options.SlidingExpiration = false;
        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async cookieContext =>
            {
                if (cookieContext.Principal is null)
                {
                    cookieContext.RejectPrincipal();
                    return;
                }

                var currentUserContext = cookieContext.HttpContext.RequestServices
                    .GetRequiredService<ICurrentUserContext>();
                var snapshot = await currentUserContext.GetAsync(
                    cookieContext.Principal,
                    cookieContext.HttpContext.RequestAborted);
                if (snapshot is null)
                {
                    cookieContext.HttpContext.Items["AppSessionInvalid"] = true;
                    cookieContext.RejectPrincipal();
                    return;
                }

                currentUserContext.EnrichPrincipal(cookieContext.Principal, snapshot);
            },
            OnRedirectToLogin = redirectContext =>
            {
                redirectContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                if (redirectContext.HttpContext.Items.ContainsKey("AppSessionInvalid"))
                {
                    redirectContext.Response.Headers["X-Auth-Reason"] = "session-invalid";
                }

                return Task.CompletedTask;
            },
            OnRedirectToAccessDenied = redirectContext =>
            {
                redirectContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }
        };
    })
    .AddJwtBearer(options =>
    {
        // Ở production, Nginx kết thúc SSL; backend chỉ chạy HTTP trong mạng nội bộ.
        options.RequireHttpsMetadata = !builder.Environment.IsProduction();
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ClockSkew = TimeSpan.FromMinutes(jwtSettings.ClockSkewMinutes)
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async tokenContext =>
            {
                if (tokenContext.Principal is null)
                {
                    tokenContext.Fail("Session is invalid.");
                    return;
                }

                var currentUserContext = tokenContext.HttpContext.RequestServices
                    .GetRequiredService<ICurrentUserContext>();
                var snapshot = await currentUserContext.GetAsync(
                    tokenContext.Principal,
                    tokenContext.HttpContext.RequestAborted);
                if (snapshot is null)
                {
                    tokenContext.HttpContext.Items["AppSessionInvalid"] = true;
                    tokenContext.Fail("Session is invalid.");
                    return;
                }

                currentUserContext.EnrichPrincipal(tokenContext.Principal, snapshot);
            },
            OnChallenge = challengeContext =>
            {
                if (challengeContext.HttpContext.Items.ContainsKey("AppSessionInvalid"))
                {
                    challengeContext.Response.Headers["X-Auth-Reason"] = "session-invalid";
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddScoped<ICurrentUserContext, CurrentUserContext>();
builder.Services.AddScoped<IAppAuthenticationService, AppAuthenticationService>();
builder.Services.AddScoped<IMembershipAdministrationService, MembershipAdministrationService>();
builder.Services.AddScoped<ISecurityAuditQueryService, SecurityAuditQueryService>();
builder.Services.AddScoped<IUserAdministrationQueryService, UserAdministrationQueryService>();
builder.Services.AddScoped<IPermissionGroupQueryService, PermissionGroupQueryService>();
builder.Services.AddScoped<IPermissionPageComponentQueryService, PermissionPageComponentQueryService>();
builder.Services.AddScoped<IPermissionMappingMutationService, PermissionMappingMutationService>();
builder.Services.AddOptions<AuthBootstrapOptions>()
    .Bind(Configuration.GetSection(AuthBootstrapOptions.SectionName));
builder.Services.AddScoped<IAuthBootstrapProvisioner, AuthBootstrapProvisioner>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddSingleton<IPermissionChangeNotifier, PermissionChangeNotifier>();
builder.Services.AddNotificationsModule(Configuration);
builder.Services.AddAuthorization(options =>
{
    foreach (var permission in Permissions.All)
    {
        options.AddPolicy(permission, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.Requirements.Add(new PermissionRequirement(permission));
        });
    }
});

var allowedOrigins = Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});
MapsterConfig.Register(TypeAdapterConfig.GlobalSettings);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Chỉ tin proxy đến từ mạng bridge của Docker.
    options.KnownIPNetworks.Add(new System.Net.IPNetwork(System.Net.IPAddress.Parse("172.16.0.0"), 12));
    options.KnownIPNetworks.Add(new System.Net.IPNetwork(System.Net.IPAddress.Parse("10.0.0.0"), 8));
});

var app = builder.Build();

var configuredDatabaseInitializationMode = Configuration["DatabaseInitialization:Mode"];
var databaseInitializationMode = DatabaseInitializationModeParser.Parse(
    configuredDatabaseInitializationMode,
    builder.Environment.IsDevelopment());
var shouldMigrate = DatabaseInitializationModeParser.RequiresMigration(databaseInitializationMode);
var shouldSeedReference = DatabaseInitializationModeParser.IncludesReferenceSeed(databaseInitializationMode);
var shouldSeedDemo = DatabaseInitializationModeParser.IncludesDemoSeed(databaseInitializationMode);
var databaseInitializationEnvironments = DeploymentConfigurationContract.GetDatabaseInitializationEnvironments(
    Configuration,
    databaseBinding,
    requireExplicitTarget: shouldMigrate);
var allowDemoData = Configuration.GetValue<bool>("DatabaseInitialization:AllowDemoData");
var demoOwnerUsername = Configuration["DatabaseInitialization:DemoOwnerUsername"];
DatabaseInitializationModeParser.ValidateForEnvironment(
    databaseInitializationMode,
    builder.Environment.IsProduction(),
    databaseInitializationEnvironments,
    allowDemoData);
DeploymentConfigurationContract.ValidateDemoDatabaseTarget(
    databaseInitializationMode,
    databaseBinding);
var databaseInitializationOnly = Configuration.GetValue<bool>("DatabaseInitialization:RunOnly");
var authBootstrapEnabled = Configuration.GetValue<bool>($"{AuthBootstrapOptions.SectionName}:Enabled");

if (databaseInitializationOnly && !shouldMigrate)
{
    throw new InvalidOperationException(
        "DatabaseInitialization:RunOnly requires Mode=Migrate, MigrateAndReference, or MigrateAndDemo.");
}

if (authBootstrapEnabled
    && (!databaseInitializationOnly || !shouldMigrate || !shouldSeedReference || shouldSeedDemo))
{
    throw new InvalidOperationException(
        "AuthBootstrap requires a one-shot RunOnly migration with reference seed and no demo seed.");
}

if (shouldMigrate)
{
    await DatabaseInitializationRunner.RunAsync(
        databaseBinding,
        databaseInitializationMode,
        shouldSeedReference,
        shouldSeedDemo,
        shouldSeedDemo
            ? new DemoWorkbookSeedOptions(demoOwnerUsername, AutoResolveOwner: true)
            : null);
}

if (authBootstrapEnabled)
{
    await using var bootstrapScope = app.Services.CreateAsyncScope();
    var provisioner = bootstrapScope.ServiceProvider.GetRequiredService<IAuthBootstrapProvisioner>();
    var result = await provisioner.ProvisionAsync();
    Log.Information(
        "Trusted owner bootstrap completed with outcome {Outcome} for account {AccountId}.",
        result.Outcome,
        result.AccountId);
}

if (databaseInitializationOnly)
{
    Log.Information("Database initialization completed. Exiting migrator process.");
    return;
}

app.UseForwardedHeaders();
app.UseRequestLocalization();

app.UseCors("AllowFrontend");
app.UseResponseCompression();

// Cấu hình HTTP request pipeline.
if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseRateLimiter();

app.UseAuthentication();
app.UseMiddleware<CookieAntiforgeryMiddleware>();
app.UseMiddleware<PasswordChangeRequiredMiddleware>();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");
if (qaFixtureIdentity is not null)
{
    app.MapGet(
            "/internal/qa/database-identity",
            async (CancellationToken cancellationToken) =>
            {
                var confirmed = await QaFixtureIdentityContract.ConfirmDatabaseAsync(
                    databaseBinding.ConnectionString,
                    qaFixtureIdentity,
                    cancellationToken);

                return confirmed is null
                    ? Results.Problem(
                        title: "QA database identity could not be confirmed.",
                        statusCode: StatusCodes.Status503ServiceUnavailable)
                    : Results.Ok(confirmed);
            })
        .AllowAnonymous()
        .ExcludeFromDescription();
}
app.MapHub<PermissionHub>("/hubs/permissions");
app.MapNotificationsModule();

app.Run();
