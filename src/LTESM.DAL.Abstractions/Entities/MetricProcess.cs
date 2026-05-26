namespace LTESM.DAL.Abstractions.Entities;

public class MetricProcess
{
    public long Id { get; set; }

    public long MetricId { get; set; }

    public int ProcessId { get; set; }

    public required string Name { get; set; } = string.Empty;

    public DateTimeOffset? StartedAtUtc { get; set; }

    public long? WorkingSetBytes { get; set; }

    public Metric Metric { get; set; } = null!;
}
