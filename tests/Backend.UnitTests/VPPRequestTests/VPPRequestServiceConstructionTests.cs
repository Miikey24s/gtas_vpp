using gtas_vpp_be.Service.Domain;
using gtas_vpp_be.Service.Exceptions;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Helpers.Context;
using gtas_vpp_be.Service.Services;
using gtas_vpp_be.Model.VPP;
using gtas_vpp_be.Tests.TestSupport;
using gtas_vpp_shared.DTOs.Req.VPP;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace gtas_vpp_be.Tests.VppItemRequestTests;

public sealed class VPPRequestServiceConstructionTests
{
    [Fact]
    public async Task Query_UsesInjectedScopedUnitOfWork()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var scopedUow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        scopedUow.SetupGet(unitOfWork => unitOfWork.VPPContext).Returns(context);
        var service = CreateService(scopedUow.Object);

        var orders = await service.GetMyOrdersAsync(5615, null, null, null);

        Assert.Empty(orders);
        scopedUow.VerifyGet(item => item.VPPContext, Times.Once);
    }

    [Fact]
    public async Task Transaction_UsesInjectedScopedUnitOfWork()
    {
        var scopedUow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        scopedUow.Setup(unitOfWork => unitOfWork.BeginTransactionAsync()).Returns(Task.CompletedTask);
        scopedUow.Setup(unitOfWork => unitOfWork.RollbackAsync()).Returns(Task.CompletedTask);
        var periodService = new Mock<IVppPeriodService>(MockBehavior.Strict);
        periodService
            .Setup(service => service.AdvanceDuePeriodsAsync("77500", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        periodService
            .Setup(service => service.GetAsync(
                "77500",
                new Period(2026, 4),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((VppPeriod?)null);
        var service = CreateService(scopedUow.Object, periodService.Object);

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

        Assert.Contains("không tồn tại", exception.Message, StringComparison.Ordinal);
        scopedUow.Verify(item => item.BeginTransactionAsync(), Times.Once);
        scopedUow.Verify(item => item.RollbackAsync(), Times.Once);
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

        Assert.Contains("builder.Services.AddHttpContextAccessor();", programRegistrationLines);
        Assert.Contains("builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();", programRegistrationLines);
        Assert.Contains("builder.Services.AddScoped<IDynamicDbContextFactory, DynamicDbContextFactory>();", programRegistrationLines);
        Assert.Contains("builder.Services.AddScoped<IUserNameResolver, UserNameResolver>();", programRegistrationLines);
        Assert.Contains("builder.Services.AddScoped<IVPPRequestService, VPPRequestService>();", programRegistrationLines);
        Assert.Contains("builder.Services.AddScoped<IOrderQuantityLimitService, OrderQuantityLimitService>();", programRegistrationLines);
        Assert.Contains("builder.Services.AddScoped<IVppPeriodService, VppPeriodService>();", programRegistrationLines);
        Assert.DoesNotContain("builder.Services.Configure<JiraSettings>(Configuration.GetSection(\"JiraSettings\"));", programRegistrationLines);
        Assert.DoesNotContain("builder.Services.AddSingleton<IEnvironmentResolver, EnvironmentResolver>();", programRegistrationLines);
        Assert.DoesNotContain("builder.Services.AddScoped<IUnitOfWorkFactory, UnitOfWorkFactory>();", programRegistrationLines);
        Assert.DoesNotContain("builder.Services.AddScoped<IBaseServices, BaseServices>();", programRegistrationLines);

        var constructorParameterTypes = typeof(VPPRequestService)
            .GetConstructors()
            .Single()
            .GetParameters()
            .Select(parameter => parameter.ParameterType)
            .ToHashSet();

        Assert.Equal(
            new[]
                {
                    typeof(IUnitOfWork),
                    typeof(IDateTimeProvider),
                    typeof(IConfiguration),
                    typeof(PeriodCalculator),
                    typeof(VppRequestPolicy),
                    typeof(IVppPeriodService),
                    typeof(IOrderQuantityLimitService)
                }
                .OrderBy(type => type.FullName),
            constructorParameterTypes.OrderBy(type => type.FullName));

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
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddSingleton(serviceProvider => VppRequestPolicy.FromConfiguration(
            serviceProvider.GetRequiredService<IConfiguration>()));
        services.AddSingleton(serviceProvider => new PeriodCalculator(
            serviceProvider.GetRequiredService<VppRequestPolicy>().DeadlineDay));
        services.AddSingleton<PeriodScheduleCalculator>();
        services.AddScoped<IUserNameResolver, UserNameResolver>();
        services.AddScoped<IDynamicDbContextFactory, DynamicDbContextFactory>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IOrderQuantityLimitService, OrderQuantityLimitService>();
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

    private static VPPRequestService CreateService(
        IUnitOfWork scopedUow,
        IVppPeriodService? periodService = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["VPPDeadlineDay"] = "5"
            })
            .Build();

        return new VPPRequestService(
            scopedUow,
            new FakeDateTimeProvider(new DateTime(2026, 4, 1, 9, 0, 0)),
            configuration,
            periodService: periodService);
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
