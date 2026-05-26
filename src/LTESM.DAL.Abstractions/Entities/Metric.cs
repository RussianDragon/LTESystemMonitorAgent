namespace LTESM.DAL.Abstractions.Entities;

public class Metric
{
    public long Id { get; set; }

    public DateTimeOffset CollectedAtUtc { get; set; }

    public required string Hostname { get; set; } = string.Empty;

    public required string WindowsVersion { get; set; } = string.Empty;

    public long UptimeSeconds { get; set; }

    public double CpuUsagePercent { get; set; }

    public double RamUsagePercent { get; set; }

    public long TotalMemoryBytes { get; set; }

    public long AvailableMemoryBytes { get; set; }

    public ICollection<MetricIpAddress> IpAddresses { get; set; } = new List<MetricIpAddress>();

    public ICollection<MetricDiskSpace> DiskSpaces { get; set; } = new List<MetricDiskSpace>();

    public ICollection<MetricProcess> RunningProcesses { get; set; } = new List<MetricProcess>();

    public ICollection<MetricMonitoredProcess> MonitoredProcesses { get; set; } = new List<MetricMonitoredProcess>();

    public MetricOutboxMessage? OutboxMessage { get; set; }
}
