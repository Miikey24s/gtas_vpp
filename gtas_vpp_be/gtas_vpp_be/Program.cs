using gtas_vpp_be.Authorization;
using gtas_vpp_be.Mappings;
using gtas_vpp_be.Middleware;
using gtas_vpp_be.Notifications;
using gtas_vpp_be.Model;
using gtas_vpp_be.Service.Domain;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Helpers.Context;
using gtas_vpp_be.Service.Services;
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
using Microsoft.Data.SqlClient;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
var dotEnvValues = LoadDotEnvValues(builder.Environment.ContentRootPath);
if (dotEnvValues.Count > 0)
{
    if (dotEnvValues.TryGetValue("JWT_KEY", out var envJwtKey) && !string.IsNullOrWhiteSpace(envJwtKey))
    {
        dotEnvValues["JwtSettings:Key"] = envJwtKey;
    }
    if (dotEnvValues.TryGetValue("PASSWORD_ENCRYPTION_KEY", out var passwordEncryptionKey)
        && !string.IsNullOrWhiteSpace(passwordEncryptionKey))
    {
        dotEnvValues["PasswordEncryption:Key"] = passwordEncryptionKey;
    }
    if (dotEnvValues.TryGetValue("REPORT_INSIGHTS_ENABLED", out var reportInsightsEnabled)
        && !string.IsNullOrWhiteSpace(reportInsightsEnabled))
    {
        dotEnvValues["ReportInsights:Enabled"] = reportInsightsEnabled;
    }
    builder.Configuration.AddInMemoryCollection(dotEnvValues);
}

var localConnectionOverrides = GetLocalDevelopmentConnectionOverrides(builder.Environment, builder.Configuration);
if (localConnectionOverrides.Count > 0)
{
    builder.Configuration.AddInMemoryCollection(localConnectionOverrides);
}

var Configuration = builder.Configuration;
var defaultDatabaseEnvironment = GetDefaultDatabaseEnvironment(builder.Environment, Configuration);
var defaultConnectionString = Configuration.GetConnectionString(defaultDatabaseEnvironment)
    ?? throw new InvalidOperationException(
        $"Connection string '{defaultDatabaseEnvironment}' is required for this environment.");

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 50 * 1024 * 1024; // 50MB
});

// Initialize Config with the application configuration
Config.Initialize(Configuration);
var jwtKey = Config.JwtSettings.Key;
var jwtIssuer = Config.JwtSettings.Issuer;
var jwtAudience = Config.JwtSettings.Audience;
if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException("JwtSettings:Key must contain at least 32 UTF-8 bytes.");
}

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
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
//builder.Services.AddOpenApi();

builder.Services.AddDbContext<VPPMigrationDbContext>(
    (sp, o) =>
    {
        o.UseSqlServer(
            defaultConnectionString,
            action => action.MigrationsAssembly(Config.DatabaseSettings.MigrationsAssembly));
    }
);
builder.Services.AddDbContext<VPPContext>(
    (sp, o) =>
    {
        o.UseSqlServer(defaultConnectionString);
    }
);

builder.Services.AddHttpContextAccessor();
builder.Services.Configure<JiraSettings>(Configuration.GetSection("JiraSettings"));
builder.Services.AddOptions<PasswordEncoderOptions>()
    .Bind(builder.Configuration.GetSection("PasswordEncryption"))
    .Validate(options => !string.IsNullOrWhiteSpace(options.Key),
        "PasswordEncryption:Key is required for legacy GTAS_MENU compatibility.")
    .ValidateOnStart();
builder.Services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
builder.Services.AddSingleton<IEnvironmentResolver, EnvironmentResolver>();
builder.Services.AddSingleton<IPasswordEncoder, TripleDesPasswordEncoder>();
builder.Services.AddSingleton(sp => new PeriodCalculator(
    sp.GetRequiredService<IConfiguration>().GetValue("VPPDeadlineDay", 5)));
builder.Services.AddScoped<IUserNameResolver, UserNameResolver>();
builder.Services.AddScoped<IDynamicDbContextFactory, DynamicDbContextFactory>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IUnitOfWorkFactory, UnitOfWorkFactory>();
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IStoredProcedureExecutor, StoredProcedureExecutor>();
builder.Services.AddScoped<IBaseServices, BaseServices>();
builder.Services.AddScoped<IVPPRequestService, VPPRequestService>();
builder.Services.AddScoped<IVPPPriceService, VPPPriceService>();
builder.Services.AddScoped<IPriceListService, PriceListService>();
builder.Services.AddScoped<IPeriodSettlementService, PeriodSettlementService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddOptions<ReportInsightsOptions>()
    .Bind(Configuration.GetSection(ReportInsightsOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.Model), "ReportInsights:Model is required.")
    .Validate(options => options.TimeoutSeconds is >= 5 and <= 60,
        "ReportInsights:TimeoutSeconds must be between 5 and 60.")
    .Validate(options => options.MaxOutputTokens is >= 300 and <= 1_500,
        "ReportInsights:MaxOutputTokens must be between 300 and 1500.")
    .ValidateOnStart();
builder.Services.AddHttpClient<IReportInsightService, ReportInsightService>(client =>
{
    client.BaseAddress = new Uri("https://api.openai.com/");
    client.Timeout = Timeout.InfiniteTimeSpan;
});
builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddSignalR();
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
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(Config.JwtSettings.ClockSkewMinutes)
        };
    });

builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
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

var databaseInitializationMode = Configuration["DatabaseInitialization:Mode"]
    ?? (builder.Environment.IsDevelopment() ? "MigrateAndSeed" : "None");
var databaseInitializationOnly = Configuration.GetValue<bool>("DatabaseInitialization:RunOnly");
var shouldMigrate = databaseInitializationMode.Equals("Migrate", StringComparison.OrdinalIgnoreCase)
    || databaseInitializationMode.Equals("MigrateAndSeed", StringComparison.OrdinalIgnoreCase);
var shouldSeed = databaseInitializationMode.Equals("MigrateAndSeed", StringComparison.OrdinalIgnoreCase);

if (!shouldMigrate && !databaseInitializationMode.Equals("None", StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException(
        $"Unsupported DatabaseInitialization:Mode '{databaseInitializationMode}'. " +
        "Expected None, Migrate, or MigrateAndSeed.");
}

if (databaseInitializationOnly && !shouldMigrate)
{
    throw new InvalidOperationException(
        "DatabaseInitialization:RunOnly requires Mode=Migrate or Mode=MigrateAndSeed.");
}

if (shouldMigrate)
{
    await InitializeDatabasesAsync(Configuration, defaultDatabaseEnvironment, shouldSeed);
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
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapHub<PermissionHub>("/hubs/permissions");
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();

static async Task InitializeDatabasesAsync(
    IConfiguration configuration,
    string defaultDatabaseEnvironment,
    bool shouldSeed)
{
    var configuredEnvironments = configuration
        .GetSection("DatabaseInitialization:Environments")
        .Get<string[]>()
        ?.Where(environment => !string.IsNullOrWhiteSpace(environment))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    var environments = configuredEnvironments is { Length: > 0 }
        ? configuredEnvironments
        : new[] { defaultDatabaseEnvironment };

    var targets = environments
        .Select(environment => new
        {
            Environment = environment,
            ConnectionString = configuration.GetConnectionString(environment)
                ?? throw new InvalidOperationException(
                    $"Connection string '{environment}' is required for database initialization.")
        })
        .GroupBy(
            target => GetDatabaseIdentity(target.ConnectionString),
            StringComparer.OrdinalIgnoreCase)
        .Select(group => new
        {
            Environments = string.Join(", ", group.Select(target => target.Environment)),
            ConnectionString = group.First().ConnectionString
        });

    foreach (var target in targets)
    {
        const int maxRetries = 5;
        for (var retry = 0; retry < maxRetries; retry++)
        {
            try
            {
                await using var migrationLock = await AcquireMigrationLockAsync(target.ConnectionString);
                var optionsBuilder = new DbContextOptionsBuilder<VPPMigrationDbContext>();
                optionsBuilder.UseSqlServer(
                    target.ConnectionString,
                    action => action.MigrationsAssembly(Config.DatabaseSettings.MigrationsAssembly));

                await using var dbContext = new VPPMigrationDbContext(optionsBuilder.Options);
                Console.WriteLine($"[Migration] Applying migration for environment(s) {target.Environments}...");
                await dbContext.Database.MigrateAsync();
                if (shouldSeed)
                {
                    await SeedData.Seed(dbContext);
                }

                Console.WriteLine($"[Migration] Environment(s) {target.Environments} completed successfully.");
                break;
            }
            catch (Exception ex)
            {
                if (retry == maxRetries - 1)
                {
                    Console.WriteLine(
                        $"[Migration] Migration for {target.Environments} failed after {maxRetries} attempts. " +
                        $"Exception: {ex.Message}");
                    throw;
                }

                Console.WriteLine(
                    $"[Migration] DB {target.Environments} is not ready, " +
                    $"retrying ({retry + 1}/{maxRetries}) in 5 seconds...");
                await Task.Delay(5000);
            }
        }
    }
}

static string GetDatabaseIdentity(string connectionString)
{
    var builder = new SqlConnectionStringBuilder(connectionString);
    return $"{builder.DataSource}|{builder.InitialCatalog}";
}

static string GetDefaultDatabaseEnvironment(IHostEnvironment environment, IConfiguration configuration)
{
    var configuredEnvironment = configuration["DatabaseSettings:DefaultEnvironment"];
    var databaseEnvironment = string.IsNullOrWhiteSpace(configuredEnvironment)
        ? environment.IsDevelopment()
            ? nameof(Config.EnvType.TestEnv)
            : nameof(Config.EnvType.LiveEnv)
        : configuredEnvironment;

    if (!Enum.TryParse<Config.EnvType>(databaseEnvironment, ignoreCase: true, out var parsedEnvironment))
    {
        throw new InvalidOperationException(
            $"Unsupported DatabaseSettings:DefaultEnvironment '{databaseEnvironment}'. " +
            $"Expected {nameof(Config.EnvType.TestEnv)} or {nameof(Config.EnvType.LiveEnv)}.");
    }

    return parsedEnvironment.ToString();
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

static Dictionary<string, string?> LoadDotEnvValues(string contentRootPath)
{
    var dotEnvPath = Path.GetFullPath(Path.Combine(contentRootPath, "..", "..", ".env"));
    if (!File.Exists(dotEnvPath))
    {
        return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    }

    var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    foreach (var rawLine in File.ReadLines(dotEnvPath))
    {
        var line = rawLine.Trim();
        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal))
        {
            continue;
        }

        var separatorIndex = line.IndexOf('=');
        if (separatorIndex <= 0)
        {
            continue;
        }

        var key = line[..separatorIndex].Trim();
        if (string.IsNullOrWhiteSpace(key))
        {
            continue;
        }

        var value = line[(separatorIndex + 1)..].Trim().Trim('"');
        values[key] = value;
    }

    return values;
}

static Dictionary<string, string?> GetLocalDevelopmentConnectionOverrides(IHostEnvironment environment, IConfiguration configuration)
{
    if (!environment.IsDevelopment())
    {
        return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    }

    if (configuration.GetValue<bool>("DISABLE_DOCKER_DB_OVERRIDE", false))
    {
        return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    }

    var currentTestEnv = configuration.GetConnectionString(nameof(Config.EnvType.TestEnv));
    var dbSaPassword = configuration["DB_SA_PASSWORD"];

    if (string.IsNullOrWhiteSpace(dbSaPassword)
        || string.IsNullOrWhiteSpace(currentTestEnv)
        || !currentTestEnv.Contains("Trusted_Connection=True", StringComparison.OrdinalIgnoreCase))
    {
        return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    }

    var localDockerConnection = $"Server=127.0.0.1,1433;Database=GTAS_VPP_LIVE;User Id=sa;Password={dbSaPassword};TrustServerCertificate=True;Encrypt=False;";

    return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
    {
        [$"ConnectionStrings:{nameof(Config.EnvType.TestEnv)}"] = localDockerConnection
    };
}
