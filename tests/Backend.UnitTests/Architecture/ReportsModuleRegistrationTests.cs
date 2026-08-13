using gtas_vpp_be.Service.Helpers.Context;
using gtas_vpp_be.Service.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace gtas_vpp_be.Tests.Architecture;

public sealed class ReportsModuleRegistrationTests
{
    [Fact]
    public void AddReportsModule_ResolvesReportServices()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ReportInsights:TimeoutSeconds"] = "30",
                ["ReportInsights:MaxOutputTokens"] = "800",
                ["ReportInsights:MaxProvidersPerRequest"] = "4"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddDbContext<VPPContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddReportsModule(configuration);

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        Assert.IsType<ReportService>(scope.ServiceProvider.GetRequiredService<IReportService>());
        Assert.IsType<CurrentSettlementReportReader>(
            scope.ServiceProvider.GetRequiredService<ICurrentSettlementReportReader>());
        Assert.IsType<ReportInsightService>(
            scope.ServiceProvider.GetRequiredService<IReportInsightService>());
    }
}
