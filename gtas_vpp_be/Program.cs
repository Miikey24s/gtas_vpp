using gtas_vpp_be.Authorization;
using gtas_vpp_be.AI.DependencyInjection;
using gtas_vpp_be.AI.KeyManagement.Interfaces;
using gtas_vpp_be.Mappings;
using gtas_vpp_be.Middleware;
using gtas_vpp_be.Model;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Helpers.Context;
using gtas_vpp_be.Service.Services;
using gtas_vpp_shared.Constants;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);
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
var aiProvider = Configuration["AISettings:Provider"] ?? "Ollama";
var isGoogleAiProvider = string.Equals(aiProvider, "Google", StringComparison.OrdinalIgnoreCase);

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
builder.Services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
builder.Services.AddSingleton<IEnvironmentResolver, EnvironmentResolver>();
builder.Services.AddScoped<IUserNameResolver, UserNameResolver>();
builder.Services.AddScoped<IDynamicDbContextFactory, DynamicDbContextFactory>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IUnitOfWorkFactory, UnitOfWorkFactory>();
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IStoredProcedureExecutor, StoredProcedureExecutor>();
builder.Services.AddScoped<IBaseServices, BaseServices>();
builder.Services.AddScoped<IVPPRequestService, VPPRequestService>();
builder.Services.AddGtasAIServices(Configuration);
if (isGoogleAiProvider)
{
    builder.Services.AddGeminiKeyManagement();
}
builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

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
    // Clearing known networks and proxies allows it to work behind Nginx in Docker Compose
    // where the proxy IP might be dynamic.
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// Legacy Gemini key management is only initialized when the Google provider is explicitly enabled.
var rawKeys = Configuration["AISettings:GeminiApiKeys"];
if (isGoogleAiProvider && !string.IsNullOrWhiteSpace(rawKeys))
{
    var keys = rawKeys.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    
    var rawAccounts = Configuration["AISettings:GeminiApiAccounts"];
    var accounts = !string.IsNullOrWhiteSpace(rawAccounts) 
        ? rawAccounts.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) 
        : new string[0];

    var keyRotationService = app.Services.GetRequiredService<IApiKeyRotationService>();
    keyRotationService.InitializeKeys(keys);
    
    var geminiKeyManager = app.Services.GetRequiredService<gtas_vpp_be.AI.KeyManagement.Interfaces.IGeminiKeyManager>();
    geminiKeyManager.InitializeKeys(keys, accounts);
}

app.UseForwardedHeaders();

app.UseCors("AllowFrontend");

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

// AI Database Migration (AIDbContext)
var aiConstr = Configuration.GetConnectionString(nameof(Config.EnvType.TestEnv));
if (!string.IsNullOrEmpty(aiConstr))
{
    var aiOptionsBuilder = new DbContextOptionsBuilder<gtas_vpp_be.AI.Data.AIDbContext>();
    aiOptionsBuilder.UseSqlServer(aiConstr, action => action.MigrationsAssembly("gtas_vpp_be.AI"));

    using var aiDbContext = new gtas_vpp_be.AI.Data.AIDbContext(aiOptionsBuilder.Options);
    
    int maxRetries = 5;
    for (int retry = 0; retry < maxRetries; retry++)
    {
        try
        {
            Console.WriteLine("[Migration] Applying AI database migration...");
            aiDbContext.Database.Migrate();
            Console.WriteLine("[Migration] AI database migration completed successfully.");
            break;
        }
        catch (Exception ex)
        {
            if (retry == maxRetries - 1)
            {
                Console.WriteLine($"[Migration] AI migration failed after {maxRetries} attempts. Exception: {ex.Message}");
                throw;
            }
            Console.WriteLine($"[Migration] AI DB is not ready, retrying ({retry + 1}/{maxRetries}) in 5 seconds...");
            await Task.Delay(5000);
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
