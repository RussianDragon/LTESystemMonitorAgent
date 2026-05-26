using LTESM.DAL.Abstractions.Entities;
using LTESystemMetricDelivery.Abstractions.Models;

namespace LTESystemOutbox;

internal class MetricPayloadFactory
{
    public MetricPayload Create(Metric metric)
    {
        return new MetricPayload(
            metric.CollectedAtUtc,
            metric.Hostname,
            metric.WindowsVersion,
            metric.UptimeSeconds,
            metric.CpuUsagePercent,
            metric.RamUsagePercent,
            metric.TotalMemoryBytes,
            metric.AvailableMemoryBytes,
            metric.IpAddresses.Select(ipAddress => new MetricIpAddressPayload(
                ipAddress.Address,
                ipAddress.AddressFamily,
                ipAddress.NetworkInterfaceName)).ToArray(),
            metric.DiskSpaces.Select(diskSpace => new MetricDiskSpacePayload(
                diskSpace.Name,
                diskSpace.VolumeLabel,
                diskSpace.DriveFormat,
                diskSpace.TotalSpaceBytes,
                diskSpace.FreeSpaceBytes)).ToArray(),
            metric.RunningProcesses.Select(process => new MetricProcessPayload(
                process.ProcessId,
                process.Name,
                process.StartedAtUtc,
                process.WorkingSetBytes)).ToArray(),
            metric.MonitoredProcesses.Select(process => new MetricMonitoredProcessPayload(
                process.Name,
                process.IsRunning,
                process.MatchedProcessCount)).ToArray());
    }
}
