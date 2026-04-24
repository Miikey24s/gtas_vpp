using gtas_vpp_be.Mappings;
using gtas_vpp_be.Model;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Helpers.Context;
using gtas_vpp_be.Service.Services;
using Mapster;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var Configuration = builder.Configuration;

// Initialize Config with the application configuration
Config.Initialize(Configuration);

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
builder.Services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
builder.Services.AddScoped<IDynamicDbContextFactory, DynamicDbContextFactory>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IUnitOfWorkFactory, UnitOfWorkFactory>();
builder.Services.AddScoped<IBaseServices, BaseServices>();
builder.Services.AddScoped<IBussinessService, BussinessService>();
builder.Services.AddScoped<IVPPRequestService, VPPRequestService>();
builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = true;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = Config.JwtSettings.Issuer,
            ValidAudience = Config.JwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Config.JwtSettings.Key)),
            ClockSkew = TimeSpan.FromMinutes(Config.JwtSettings.ClockSkewMinutes)
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
MapsterConfig.Register(TypeAdapterConfig.GlobalSettings);
var app = builder.Build();

app.UseCors("AllowAll");

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<VPPMigrationDbContext>();
    dbContext.Database.Migrate();

    await SeedData.Seed(dbContext);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    //app.MapOpenApi();
}
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
