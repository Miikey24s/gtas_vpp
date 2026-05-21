using gtas_vpp_be.Authorization;
using gtas_vpp_be.Mappings;
using gtas_vpp_be.Middleware;
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

var builder = WebApplication.CreateBuilder(args);
var dotEnvValues = LoadDotEnvValues(builder.Environment.ContentRootPath);
if (dotEnvValues.Count > 0)
{
    builder.Configuration.AddInMemoryCollection(dotEnvValues);
}

var localConnectionOverrides = GetLocalDevelopmentConnectionOverrides(builder.Environment, builder.Configuration);
if (localConnectionOverrides.Count > 0)
{
    builder.Configuration.AddInMemoryCollection(localConnectionOverrides);
}

var Configuration = builder.Configuration;

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 50 * 1024 * 1024; // 50MB
});

// Initialize Config with the application configuration
Config.Initialize(Configuration);
var jwtKey = Config.JwtSettings.Key;
var jwtIssuer = Config.JwtSettings.Issuer;
var jwtAudience = Config.JwtSettings.Audience;

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
        //var constr = Configuration.GetConnectionString("TestEnv");
        var constr = Configuration.GetConnectionString(nameof(Config.EnvType.TestEnv));
        o.UseSqlServer(constr, action => action.MigrationsAssembly(Config.DatabaseSettings.MigrationsAssembly));
    }
);
builder.Services.AddDbContext<VPPContext>(
    (sp, o) =>
    {
        var constr = Configuration.GetConnectionString(nameof(Config.EnvType.TestEnv));
        o.UseSqlServer(constr);
    }
);

builder.Services.AddHttpContextAccessor();
builder.Services.Configure<JiraSettings>(Configuration.GetSection("JiraSettings"));
builder.Services.Configure<PasswordEncoderOptions>(builder.Configuration.GetSection("PasswordEncryption"));
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
builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

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
    options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(System.Net.IPAddress.Parse("172.16.0.0"), 12));
    options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(System.Net.IPAddress.Parse("10.0.0.0"), 8));
});

var app = builder.Build();

app.UseForwardedHeaders();

app.UseCors("AllowFrontend");
app.UseResponseCompression();

// WARNING: Running Migrate() automatically in Program.cs with multiple replicas (Docker Swarm/K8s) can cause race conditions.
// The best solution is to use a separate container that only runs "dotnet ef database update" and then exits.
// Below is the "safest possible" approach if kept in Program.cs: applying a Retry policy.
var environments = new[] { Config.EnvType.TestEnv, Config.EnvType.LiveEnv };
foreach (var env in environments)
{
    var constr = Configuration.GetConnectionString(env.ToString());
    if (!string.IsNullOrEmpty(constr))
    {
        var optionsBuilder = new DbContextOptionsBuilder<VPPMigrationDbContext>();
        optionsBuilder.UseSqlServer(constr, action => action.MigrationsAssembly(Config.DatabaseSettings.MigrationsAssembly));

        using var dbContext = new VPPMigrationDbContext(optionsBuilder.Options);
        
        int maxRetries = 5;
        for (int retry = 0; retry < maxRetries; retry++)
        {
            try
            {
                // Add log to track progress in Docker logs
                Console.WriteLine($"[Migration] Applying migration for environment {env}...");
                dbContext.Database.Migrate();
                await SeedData.Seed(dbContext);
                Console.WriteLine($"[Migration] Environment {env} completed successfully.");
                break;
            }
            catch (Exception ex)
            {
                if (retry == maxRetries - 1)
                {
                    Console.WriteLine($"[Migration] Migration for {env} failed after {maxRetries} attempts. Exception: {ex.Message}");
                    throw;
                }
                Console.WriteLine($"[Migration] DB {env} is not ready, retrying ({retry + 1}/{maxRetries}) in 5 seconds...");
                await Task.Delay(5000);
            }
        }
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    
    // HTTPS redirection handled by Nginx reverse proxy in production
    app.UseHttpsRedirection();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

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
        [$"ConnectionStrings:{nameof(Config.EnvType.TestEnv)}"] = localDockerConnection,
        [$"ConnectionStrings:{nameof(Config.EnvType.LiveEnv)}"] = localDockerConnection
    };
}
