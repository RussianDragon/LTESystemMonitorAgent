using LTESystemMonitoring.Abstractions;
using LTESystemMonitoring.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LTESystemMonitoring;

public static class MonitoringExtensions
{
    public static IServiceCollection AddMonitoring(
        this IServiceCollection services,
        IConfigurationSection configurationSection)
    {
        var configuration = configurationSection.Get<MonitoringConfiguration>()
            ?? throw new InvalidOperationException("Configuration section 'Monitoring' is missing or invalid.");

        if (configuration.CpuSampleMilliseconds <= 0)
        {
            throw new InvalidOperationException("Configuration setting 'Monitoring:CpuSampleMilliseconds' must be greater than zero.");
        }

        if (configuration.MonitoredProcesses.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException("Configuration setting 'Monitoring:MonitoredProcesses' must not contain empty process names.");
        }

        services.Configure<MonitoringConfiguration>(configurationSection);

        services.AddScoped<IMetricCollectionService, MetricCollectionService>();

        return services;
    }
}
