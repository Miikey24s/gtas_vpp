using gtas_vpp_be.Model;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var Configuration = builder.Configuration;

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<VPPMigrationDbContext>(
    (sp, o) =>
    {
        //var constr = Configuration.GetConnectionString("TestEnv");
        var constr = Configuration.GetConnectionString(nameof(Config.EnvConfig.EnvType.TestEnv));
        o.UseSqlServer(constr, action => action.MigrationsAssembly("gtas_vpp_be.Migrations"));
    }
);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IDynamicDbContextFactory, DynamicDbContextFactory>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IUnitOfWorkFactory, UnitOfWorkFactory>();
builder.Services.AddScoped<IBaseServices, BaseServices>();
builder.Services.AddScoped<IBussinessService, BussinessService>();
builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<VPPMigrationDbContext>();
    dbContext.Database.Migrate();

    await SeedData.Seed(dbContext);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
