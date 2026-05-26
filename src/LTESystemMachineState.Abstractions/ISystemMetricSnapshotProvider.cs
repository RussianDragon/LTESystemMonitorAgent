using LTESystemMachineState.Abstractions.Models;

namespace LTESystemMachineState.Abstractions;

public interface ISystemMetricSnapshotProvider
{
    Task<SystemMetricSnapshot> CollectAsync(
        int cpuSampleMilliseconds,
        CancellationToken cancellationToken = default);
}
