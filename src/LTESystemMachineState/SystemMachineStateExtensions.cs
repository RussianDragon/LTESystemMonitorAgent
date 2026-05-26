using LTESystemMachineState.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace LTESystemMachineState;

public static class SystemMachineStateExtensions
{
    public static IServiceCollection AddSystemMachineState(this IServiceCollection services)
    {
        services.AddScoped<ISystemMetricSnapshotProvider, SystemMetricSnapshotProvider>();

        return services;
    }
}
