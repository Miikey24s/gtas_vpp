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

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(accountTokenKeysPath))
    .SetApplicationName("gtas_vpp_backend_identity");
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
//builder.Services.AddOpenApi();

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
builder.Services.Configure<JiraSettings>(Configuration.GetSection("JiraSettings"));
builder.Services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
builder.Services.AddSingleton<IEnvironmentResolver, EnvironmentResolver>();
builder.Services.AddSingleton(sp => VppRequestPolicy.FromConfiguration(
    sp.GetRequiredService<IConfiguration>()));
builder.Services.AddSingleton(sp => new PeriodCalculator(
    sp.GetRequiredService<VppRequestPolicy>().DeadlineDay));
builder.Services.AddScoped<IUserNameResolver, UserNameResolver>();
builder.Services.AddScoped<IDynamicDbContextFactory, DynamicDbContextFactory>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IUnitOfWorkFactory, UnitOfWorkFactory>();
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IBaseServices, BaseServices>();
builder.Services.AddScoped<IVPPRequestService, VPPRequestService>();
builder.Services.AddScoped<IVppPeriodService, VppPeriodService>();
builder.Services.AddHostedService<VppPeriodRecoveryWorker>();
builder.Services.AddScoped<IVPPPriceService, VPPPriceService>();
builder.Services.AddScoped<IPriceAsOfResolver, PriceAsOfResolver>();
builder.Services.AddScoped<IPriceBookWorkflowService, PriceBookWorkflowService>();
builder.Services.AddScoped<IPriceListService, PriceListService>();
builder.Services.AddScoped<IPeriodSettlementService, PeriodSettlementService>();
builder.Services.AddScoped<IVppCatalogService, VppCatalogService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddReportInsights(Configuration);
builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
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
builder.Services.AddOptions<AccountEmailOptions>()
    .Bind(Configuration.GetSection(AccountEmailOptions.SectionName))
    .Validate(options => Uri.TryCreate(options.PublicBaseUrl, UriKind.Absolute, out _),
        "EmailNotifications:PublicBaseUrl must be an absolute URL.")
    .Validate(options => options.SmtpPort is >= 1 and <= 65535,
        "EmailNotifications:SmtpPort must be a valid TCP port.")
    .Validate(options => !options.Enabled || !string.IsNullOrWhiteSpace(options.SmtpHost),
        "EmailNotifications:SmtpHost is required when email is enabled.")
    .Validate(options => !options.Enabled
            || System.Net.Mail.MailAddress.TryCreate(options.FromAddress, out _),
        "EmailNotifications:FromAddress must be a valid email address when email is enabled.")
    .ValidateOnStart();
builder.Services.AddScoped<IEmailOutboxService, EmailOutboxService>();
builder.Services.AddScoped<ICurrentMemberCompanyProvider, DefaultMemberCompanyProvider>();
builder.Services.AddScoped<SmtpAccountEmailSender>();
builder.Services.AddScoped<IAccountEmailSender, OutboxAccountEmailSender>();
builder.Services.AddHostedService<EmailOutboxWorker>();
builder.Services.AddScoped<IAccountLifecycleService, AccountLifecycleService>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
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

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        // In production, Nginx handles SSL termination; backend runs HTTP internally
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
builder.Services.AddOptions<AuthBootstrapOptions>()
    .Bind(Configuration.GetSection(AuthBootstrapOptions.SectionName));
builder.Services.AddScoped<IAuthBootstrapProvisioner, AuthBootstrapProvisioner>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddSingleton<IPermissionChangeNotifier, PermissionChangeNotifier>();
builder.Services.AddScoped<IAppNotificationService, AppNotificationService>();
builder.Services.AddSingleton<INotificationRealtimeNotifier, NotificationRealtimeNotifier>();
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
    // Only trust proxy from Docker bridge network
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
if (shouldSeedDemo && string.IsNullOrWhiteSpace(demoOwnerUsername))
{
    throw new InvalidOperationException(
        "MigrateAndDemo requires DatabaseInitialization:DemoOwnerUsername so workbook orders can be bound to an active account and primary department.");
}
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
    await InitializeDatabaseAsync(
        databaseBinding,
        databaseInitializationMode,
        shouldSeedReference,
        shouldSeedDemo,
        shouldSeedDemo ? new DemoWorkbookSeedOptions(demoOwnerUsername) : null);
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

app.UseCors("AllowFrontend");
app.UseResponseCompression();

// Configure the HTTP request pipeline.
if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseRateLimiter();

app.UseAuthentication();
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
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();

static async Task InitializeDatabaseAsync(
    DatabaseBinding databaseBinding,
    DatabaseInitializationMode initializationMode,
    bool shouldSeedReference,
    bool shouldSeedDemo,
    DemoWorkbookSeedOptions? demoSeedOptions)
{
    const int maxRetries = 5;
    for (var retry = 0; retry < maxRetries; retry++)
    {
        try
        {
            await using var migrationLock = await AcquireMigrationLockAsync(databaseBinding.ConnectionString);
            var optionsBuilder = new DbContextOptionsBuilder<VPPMigrationDbContext>();
            optionsBuilder.UseSqlServer(
                databaseBinding.ConnectionString,
                action => action.MigrationsAssembly(Config.DatabaseSettings.MigrationsAssembly));

            await using var dbContext = new VPPMigrationDbContext(optionsBuilder.Options);
            Console.WriteLine(
                $"[Migration] Applying mode {initializationMode} for environment {databaseBinding.EnvironmentName}...");
            await dbContext.Database.MigrateAsync();
            if (shouldSeedReference || shouldSeedDemo)
            {
                try
                {
                    if (shouldSeedDemo)
                    {
                        await SeedData.SeedDemo(dbContext, demoSeedOptions);
                    }
                    else
                    {
                        await SeedData.SeedReference(dbContext);
                    }
                }
                catch (Exception ex)
                {
                    throw new DatabaseInitializationException(
                        $"Database seed mode {initializationMode} failed for environment {databaseBinding.EnvironmentName}.",
                        ex);
                }
            }

            Console.WriteLine(
                $"[Migration] Mode {initializationMode} for environment {databaseBinding.EnvironmentName} completed successfully.");
            break;
        }
        catch (DatabaseInitializationException ex)
        {
            Console.WriteLine(
                $"[Migration] Deterministic initialization failure for {databaseBinding.EnvironmentName}: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            if (retry == maxRetries - 1)
            {
                Console.WriteLine(
                    $"[Migration] Migration for {databaseBinding.EnvironmentName} failed after {maxRetries} attempts. " +
                    $"Exception: {ex.Message}");
                throw;
            }

            Console.WriteLine(
                $"[Migration] DB {databaseBinding.EnvironmentName} is not ready, " +
                $"retrying ({retry + 1}/{maxRetries}) in 5 seconds...");
            await Task.Delay(5000);
        }
    }
}

static async Task<SqlConnection> AcquireMigrationLockAsync(string connectionString)
{
    var builder = new SqlConnectionStringBuilder(connectionString);
    var databaseName = builder.InitialCatalog;
    builder.InitialCatalog = "master";

    var connection = new SqlConnection(builder.ConnectionString);
    await connection.OpenAsync();

    try
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock
                @Resource = @resource,
                @LockMode = 'Exclusive',
                @LockOwner = 'Session',
                @LockTimeout = 120000;
            SELECT @result;
            """;
        command.Parameters.AddWithValue("@resource", $"GTAS_VPP_SCHEMA_INIT:{databaseName}");

        var result = Convert.ToInt32(await command.ExecuteScalarAsync());
        if (result < 0)
        {
            throw new InvalidOperationException(
                $"Could not acquire the database migration lock for {databaseName}. SQL result: {result}.");
        }

        return connection;
    }
    catch
    {
        await connection.DisposeAsync();
        throw;
    }
}

public static class DeploymentConfigurationContract
{
    public static string[] GetDatabaseInitializationEnvironments(
        IConfiguration configuration,
        DatabaseBinding databaseBinding,
        bool requireExplicitTarget)
    {
        var configuredEnvironments = configuration
            .GetSection("DatabaseInitialization:Environments")
            .Get<string[]>()
            ?.Where(environment => !string.IsNullOrWhiteSpace(environment))
            .Select(environment => environment.Trim())
            .ToArray();

        if (configuredEnvironments is not { Length: > 0 })
        {
            if (requireExplicitTarget)
            {
                throw new InvalidOperationException(
                    "DatabaseInitialization:Environments must explicitly contain the deployment " +
                    $"database binding '{databaseBinding.EnvironmentName}'.");
            }

            return [databaseBinding.EnvironmentName];
        }

        if (configuredEnvironments.Length != 1
                || !string.Equals(
                    configuredEnvironments[0],
                    databaseBinding.EnvironmentName,
                    StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "DatabaseInitialization:Environments must contain only the deployment database binding " +
                $"'{databaseBinding.EnvironmentName}'.");
        }

        return [databaseBinding.EnvironmentName];
    }

    public static void ValidateDatabaseBindingForHost(
        string hostEnvironmentName,
        bool isProduction,
        DatabaseBinding databaseBinding)
    {
        var expectedEnvironment = isProduction
            ? DatabaseBinding.LiveEnvironment
            : DatabaseBinding.TestEnvironment;
        if (!string.Equals(
                databaseBinding.EnvironmentName,
                expectedEnvironment,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Host environment '{hostEnvironmentName}' requires database binding " +
                $"'{expectedEnvironment}', but '{databaseBinding.EnvironmentName}' was configured.");
        }

        if (databaseBinding.IsTestEnvironment
            && !HasTestOrDemoDatabaseToken(databaseBinding.DatabaseName))
        {
            throw new InvalidOperationException(
                "The TestEnv binding requires a database name containing TEST or DEMO.");
        }

        if (isProduction
            && !string.Equals(
                databaseBinding.DatabaseName,
                "GTAS_VPP_LIVE",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The Production LiveEnv binding must target database 'GTAS_VPP_LIVE'.");
        }
    }

    public static void ValidateDemoDatabaseTarget(
        DatabaseInitializationMode initializationMode,
        DatabaseBinding databaseBinding)
    {
        if (!DatabaseInitializationModeParser.IncludesDemoSeed(initializationMode))
        {
            return;
        }

        if (!databaseBinding.IsTestEnvironment
            || !IsApprovedLocalDataSource(databaseBinding.DataSource)
            || !HasTestOrDemoDatabaseToken(databaseBinding.DatabaseName))
        {
            throw new InvalidOperationException(
                "Demo database initialization requires a TestEnv binding on LocalDB, a loopback SQL Server, " +
                "or the local Compose 'db' service, and a database name containing TEST or DEMO.");
        }
    }

    private static bool IsApprovedLocalDataSource(string dataSource)
    {
        var normalized = dataSource.Trim();
        if (normalized.StartsWith("tcp:", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[4..];
        }

        if (normalized.StartsWith("(localdb)\\", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var hostOrInstance = normalized.Split(',', 2)[0];
        return IsHostOrNamedInstance(hostOrInstance, "localhost")
            || IsHostOrNamedInstance(hostOrInstance, "127.0.0.1")
            || IsHostOrNamedInstance(hostOrInstance, ".")
            || IsHostOrNamedInstance(hostOrInstance, "(local)")
            || string.Equals(hostOrInstance, "::1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(hostOrInstance, "[::1]", StringComparison.OrdinalIgnoreCase)
            || string.Equals(hostOrInstance, "db", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsHostOrNamedInstance(string hostOrInstance, string allowedHost)
    {
        return string.Equals(hostOrInstance, allowedHost, StringComparison.OrdinalIgnoreCase)
            || hostOrInstance.StartsWith($"{allowedHost}\\", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasTestOrDemoDatabaseToken(string databaseName)
    {
        return databaseName
            .Split(['_', '-', '.', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(token => string.Equals(token, "TEST", StringComparison.OrdinalIgnoreCase)
                || string.Equals(token, "DEMO", StringComparison.OrdinalIgnoreCase));
    }
}
