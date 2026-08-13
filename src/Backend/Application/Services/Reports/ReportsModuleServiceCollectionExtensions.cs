using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace gtas_vpp_be.Service.Services;

public static class ReportsModuleServiceCollectionExtensions
{
    public static IServiceCollection AddReportsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<ICurrentSettlementReportReader, CurrentSettlementReportReader>();
        services.AddScoped<IReportService, ReportService>();
        services.AddReportInsights(configuration);
        return services;
    }
}
