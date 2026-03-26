using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DART.ReportGenerator.DependencyInjection;

public static class ReportGeneratorServiceCollectionExtensions
{
    public static IServiceCollection AddReportGenerator(this IServiceCollection services)
    {
        services.TryAddSingleton<WorkbookComparisonService>();
        services.TryAddSingleton<IReportGenerator, ReportGenerator>();

        return services;
    }
}
