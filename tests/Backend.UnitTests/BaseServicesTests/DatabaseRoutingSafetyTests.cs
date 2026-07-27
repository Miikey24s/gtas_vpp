using System.Security.Claims;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Helpers.Context;
using gtas_vpp_be.Service.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace gtas_vpp_be.Tests.BaseServicesTests;

public class DatabaseRoutingSafetyTests
{
    [Fact]
    public void UnitOfWork_UsesOnlyTheImmutableDeploymentBinding()
    {
        var binding = CreateTestBinding();
        var contextFactory = new DynamicDbContextFactory(binding);

        using var unitOfWork = new UnitOfWork(contextFactory);
        var actual = new SqlConnectionStringBuilder(
            unitOfWork.VPPContext.Database.GetConnectionString());

        Assert.Equal(binding.DataSource, actual.DataSource);
        Assert.Equal(binding.DatabaseName, actual.InitialCatalog);
    }

    [Fact]
    public void LegacyLiveServerClaim_CannotChangeDirectOrUnitOfWorkConnection()
    {
        var binding = CreateTestBinding();
        var request = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity([new Claim("Server", "Live")], "legacy-token"))
        };
        Assert.Equal("Live", request.User.FindFirst("Server")?.Value);

        var directOptions = new DbContextOptionsBuilder<VPPContext>()
            .UseSqlServer(binding.ConnectionString)
            .Options;
        using var directContext = new VPPContext(directOptions);
        using var unitOfWork = new UnitOfWork(new DynamicDbContextFactory(binding));

        var directIdentity = GetConnectionIdentity(
            directContext.Database.GetConnectionString());
        var unitOfWorkIdentity = GetConnectionIdentity(
            unitOfWork.VPPContext.Database.GetConnectionString());

        Assert.Equal((binding.DataSource, binding.DatabaseName), directIdentity);
        Assert.Equal(directIdentity, unitOfWorkIdentity);
        Assert.Equal(DatabaseBinding.TestEnvironment, new EnvironmentResolver(binding).Resolve());
    }

    [Fact]
    public void DatabaseRoutingInterfaces_HaveNoEnvironmentSelectorParameters()
    {
        var createContext = typeof(IDynamicDbContextFactory)
            .GetMethod(nameof(IDynamicDbContextFactory.CreateVPPContext));
        var createUnitOfWork = typeof(IUnitOfWorkFactory)
            .GetMethod(nameof(IUnitOfWorkFactory.Create));

        Assert.NotNull(createContext);
        Assert.Empty(createContext.GetParameters());
        Assert.NotNull(createUnitOfWork);
        Assert.Empty(createUnitOfWork.GetParameters());
        Assert.DoesNotContain(
            typeof(IUnitOfWork).GetMethods(),
            method => string.Equals(method.Name, "Init", StringComparison.Ordinal));
    }

    private static DatabaseBinding CreateTestBinding()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DatabaseSettings:DefaultEnvironment"] = DatabaseBinding.TestEnvironment,
                ["ConnectionStrings:TestEnv"] =
                    "Server=localhost;Database=GTAS_ENV001_BOUND;Integrated Security=True;TrustServerCertificate=True"
            })
            .Build();

        return DatabaseBinding.Create(configuration);
    }

    private static (string DataSource, string DatabaseName) GetConnectionIdentity(
        string? connectionString)
    {
        var parsed = new SqlConnectionStringBuilder(
            connectionString ?? throw new InvalidOperationException("A bound connection string is required."));
        return (parsed.DataSource, parsed.InitialCatalog);
    }
}
