namespace LTESystemMetricDelivery.Abstractions.Models;

public sealed record MetricPayload(
    DateTimeOffset CollectedAtUtc,
    string Hostname,
    string WindowsVersion,
    long UptimeSeconds,
    double CpuUsagePercent,
    double RamUsagePercent,
    long TotalMemoryBytes,
    long AvailableMemoryBytes,
    IReadOnlyCollection<MetricIpAddressPayload> IpAddresses,
    IReadOnlyCollection<MetricDiskSpacePayload> DiskSpaces,
    IReadOnlyCollection<MetricProcessPayload> RunningProcesses,
    IReadOnlyCollection<MetricMonitoredProcessPayload> MonitoredProcesses);
