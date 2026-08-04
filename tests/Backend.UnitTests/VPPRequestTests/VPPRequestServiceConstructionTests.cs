using gtas_vpp_be.Service.Domain;
using gtas_vpp_be.Service.Exceptions;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Helpers.Context;
using gtas_vpp_be.Service.Services;
using gtas_vpp_be.Tests.TestSupport;
using gtas_vpp_shared.DTOs.Req.VPP;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace gtas_vpp_be.Tests.VppItemRequestTests;

public sealed class VPPRequestServiceConstructionTests
{
    [Fact]
    public async Task Query_UsesScopedUnitOfWork_AndLeavesFactoryCreatedUnitOfWorkUntouched()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var scopedUow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        scopedUow.SetupGet(unitOfWork => unitOfWork.VPPContext).Returns(context);
        var factoryCreatedUow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var factory = CreateFactory(factoryCreatedUow.Object);
        var service = CreateService(factory.Object, scopedUow.Object);

        var orders = await service.GetMyOrdersAsync(5615, null, null, null);

        Assert.Empty(orders);
        factory.Verify(item => item.Create(), Times.Once);
        scopedUow.VerifyGet(item => item.VPPContext, Times.Once);
        VerifyFactoryCreatedUnitOfWorkWasNotUsed(factoryCreatedUow);
    }

    [Fact]
    public async Task Transaction_UsesScopedUnitOfWork_AndLeavesFactoryCreatedUnitOfWorkUntouched()
    {
        var scopedUow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        scopedUow.Setup(unitOfWork => unitOfWork.BeginTransactionAsync()).Returns(Task.CompletedTask);
        scopedUow.Setup(unitOfWork => unitOfWork.RollbackAsync()).Returns(Task.CompletedTask);
        var factoryCreatedUow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var factory = CreateFactory(factoryCreatedUow.Object);
        var service = CreateService(factory.Object, scopedUow.Object);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => service.CreateOrderAsync(
            new VppRequestCreateReqDTO
            {
                Year = 2026,
                Month = 4,
                Description = "Out-of-period characterization request",
                Items = [new VppRequestDetailItemReqDTO { VppId = Guid.NewGuid(), Qty = 1 }]
            },
            createdByUserId: 5615,
            departmentCode: "IT",
            memberCompanyCode: "77500"));

        Assert.Contains("current period", exception.Message, StringComparison.Ordinal);
        factory.Verify(item => item.Create(), Times.Once);
        scopedUow.Verify(item => item.BeginTransactionAsync(), Times.Once);
        scopedUow.Verify(item => item.RollbackAsync(), Times.Once);
        VerifyFactoryCreatedUnitOfWorkWasNotUsed(factoryCreatedUow);
    }

    [Fact]
    public void ProgramRequestRegistrationContract_BuildsAndResolvesWithStrictScopeValidation()
    {
        var programPath = Path.Combine(
            FindRepositoryRoot(),
            "src",
            "Backend",
            "Api",
            "Program.cs");
        var programRegistrationLines = File.ReadLines(programPath)
            .Select(line => line.Trim())
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();", programRegistrationLines);
        Assert.Contains("builder.Services.AddScoped<IUnitOfWorkFactory, UnitOfWorkFactory>();", programRegistrationLines);
        Assert.Contains("builder.Services.AddScoped<IBaseServices, BaseServices>();", programRegistrationLines);
        Assert.Contains("builder.Services.AddScoped<IVPPRequestService, VPPRequestService>();", programRegistrationLines);
        Assert.Contains("builder.Services.AddScoped<IVppPeriodService, VppPeriodService>();", programRegistrationLines);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DatabaseSettings:DefaultEnvironment"] = DatabaseBinding.TestEnvironment,
                ["ConnectionStrings:TestEnv"] =
                    "Server=localhost;Database=GTAS_VPP_CONSTRUCTION_TEST;Integrated Security=True;TrustServerCertificate=True",
                ["VPPDeadlineDay"] = "5"
            })
            .Build();
        var binding = DatabaseBinding.Create(configuration);
        // Program là top-level và có side effect khởi động host, nên test khóa exact
        // registration lines rồi dựng graph tối thiểu để validate constructor/lifetime.
        var services = new ServiceCollection();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton(binding);
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.Configure<JiraSettings>(configuration.GetSection("JiraSettings"));
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddSingleton<IEnvironmentResolver, EnvironmentResolver>();
        services.AddSingleton(serviceProvider => VppRequestPolicy.FromConfiguration(
            serviceProvider.GetRequiredService<IConfiguration>()));
        services.AddSingleton(serviceProvider => new PeriodCalculator(
            serviceProvider.GetRequiredService<VppRequestPolicy>().DeadlineDay));
        services.AddScoped<IUserNameResolver, UserNameResolver>();
        services.AddScoped<IDynamicDbContextFactory, DynamicDbContextFactory>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUnitOfWorkFactory, UnitOfWorkFactory>();
        services.AddScoped<IBaseServices, BaseServices>();
        services.AddScoped<IVPPRequestService, VPPRequestService>();
        services.AddScoped<IVppPeriodService, VppPeriodService>();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
        using var scope = provider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IVPPRequestService>();

        Assert.IsType<VPPRequestService>(service);
    }

    private static Mock<IUnitOfWorkFactory> CreateFactory(IUnitOfWork factoryCreatedUow)
    {
        var factory = new Mock<IUnitOfWorkFactory>(MockBehavior.Strict);
        factory.Setup(item => item.Create()).Returns(factoryCreatedUow);
        return factory;
    }

    private static VPPRequestService CreateService(
        IUnitOfWorkFactory factory,
        IUnitOfWork scopedUow)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["VPPDeadlineDay"] = "5"
            })
            .Build();

        return new VPPRequestService(
            factory,
            ServiceTestHelpers.CreateHttpContextAccessor(),
            scopedUow,
            new FakeDateTimeProvider(new DateTime(2026, 4, 1, 9, 0, 0)),
            configuration,
            ServiceTestHelpers.CreateEnvironmentResolver(),
            new UserNameResolver(),
            NullLogger<BaseServices>.Instance,
            Options.Create(new JiraSettings()));
    }

    private static void VerifyFactoryCreatedUnitOfWorkWasNotUsed(Mock<IUnitOfWork> factoryCreatedUow)
    {
        factoryCreatedUow.VerifyGet(item => item.VPPContext, Times.Never);
        factoryCreatedUow.Verify(item => item.BeginTransaction(), Times.Never);
        factoryCreatedUow.Verify(item => item.BeginTransactionAsync(), Times.Never);
        factoryCreatedUow.Verify(item => item.Commit(), Times.Never);
        factoryCreatedUow.Verify(item => item.CommitAsync(), Times.Never);
        factoryCreatedUow.Verify(item => item.Rollback(), Times.Never);
        factoryCreatedUow.Verify(item => item.RollbackAsync(), Times.Never);
        factoryCreatedUow.Verify(item => item.SaveChanges(), Times.Never);
        factoryCreatedUow.Verify(item => item.SaveChangesAsync(), Times.Never);
        factoryCreatedUow.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        factoryCreatedUow.VerifyNoOtherCalls();
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "gtas_vpp.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the GTAS VPP repository root.");
    }
}
