using LTESM.DAL.Abstractions;
using LTESM.DAL.Abstractions.Entities;
using LTESystemMachineState.Abstractions;
using LTESystemMachineState.Abstractions.Models;
using LTESystemMonitoring.Abstractions;
using LTESystemMonitoring.Configurations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LTESystemMonitoring;

public class MetricCollectionService(
    ILTEDbContext dbContext,
    IOptions<MonitoringConfiguration> options,
    ISystemMetricSnapshotProvider snapshotProvider,
    ILogger<MetricCollectionService> logger) : IMetricCollectionService
{
    public async Task CollectAndSaveAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await snapshotProvider.CollectAsync(
            options.Value.CpuSampleMilliseconds,
            cancellationToken);

        var runningProcesses = snapshot.RunningProcesses
            .Select(process => new MetricProcess
            {
                ProcessId = process.ProcessId,
                Name = process.Name,
                StartedAtUtc = process.StartedAtUtc,
                WorkingSetBytes = process.WorkingSetBytes
            })
            .ToList();

        var metric = new Metric
        {
            CollectedAtUtc = snapshot.CollectedAtUtc,
            Hostname = snapshot.Hostname,
            WindowsVersion = snapshot.WindowsVersion,
            UptimeSeconds = snapshot.UptimeSeconds,
            CpuUsagePercent = snapshot.CpuUsagePercent,
            RamUsagePercent = snapshot.RamUsagePercent,
            TotalMemoryBytes = snapshot.TotalMemoryBytes,
            AvailableMemoryBytes = snapshot.AvailableMemoryBytes,
            IpAddresses = snapshot.IpAddresses
                .Select(ipAddress => new MetricIpAddress
                {
                    Address = ipAddress.Address,
                    AddressFamily = ipAddress.AddressFamily,
                    NetworkInterfaceName = ipAddress.NetworkInterfaceName
                })
                .ToList(),
            DiskSpaces = snapshot.DiskSpaces
                .Select(diskSpace => new MetricDiskSpace
                {
                    Name = diskSpace.Name,
                    VolumeLabel = diskSpace.VolumeLabel,
                    DriveFormat = diskSpace.DriveFormat,
                    TotalSpaceBytes = diskSpace.TotalSpaceBytes,
                    FreeSpaceBytes = diskSpace.FreeSpaceBytes
                })
                .ToList(),
            RunningProcesses = runningProcesses,
            MonitoredProcesses = GetMonitoredProcesses(runningProcesses),
            OutboxMessage = new MetricOutboxMessage
            {
                CreatedAtUtc = snapshot.CollectedAtUtc,
                Status = OutboxMessageStatus.Pending
            }
        };

        dbContext.Metrics.Add(metric);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("System metric snapshot saved. MetricId: {MetricId}.", metric.Id);
    }

    private ICollection<MetricMonitoredProcess> GetMonitoredProcesses(ICollection<MetricProcess> runningProcesses)
    {
        return options.Value.MonitoredProcesses
            .Where(processName => !string.IsNullOrWhiteSpace(processName))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(processName =>
            {
                var normalizedName = NormalizeProcessName(processName);
                var matchedCount = runningProcesses.Count(process =>
                    string.Equals(NormalizeProcessName(process.Name), normalizedName, StringComparison.OrdinalIgnoreCase));

                return new MetricMonitoredProcess
                {
                    Name = processName,
                    IsRunning = matchedCount > 0,
                    MatchedProcessCount = matchedCount
                };
            })
            .ToList();
    }

    private static string NormalizeProcessName(string processName)
    {
        return Path.GetFileNameWithoutExtension(processName.Trim());
    }
}
