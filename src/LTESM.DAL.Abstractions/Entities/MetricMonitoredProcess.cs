namespace LTESM.DAL.Abstractions.Entities;

public class MetricMonitoredProcess
{
    public long Id { get; set; }

    public long MetricId { get; set; }

    public required string Name { get; set; } = string.Empty;

    public bool IsRunning { get; set; }

    public int MatchedProcessCount { get; set; }

    public Metric Metric { get; set; } = null!;
}
