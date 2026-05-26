namespace LTESystemMachineState.Abstractions.Models;

public sealed record SystemMetricSnapshot(
    DateTimeOffset CollectedAtUtc,
    string Hostname,
    string WindowsVersion,
    long UptimeSeconds,
    double CpuUsagePercent,
    double RamUsagePercent,
    long TotalMemoryBytes,
    long AvailableMemoryBytes,
    IReadOnlyCollection<SystemMetricIpAddress> IpAddresses,
    IReadOnlyCollection<SystemMetricDiskSpace> DiskSpaces,
    IReadOnlyCollection<SystemMetricProcess> RunningProcesses);
