namespace LTESM.DAL.Abstractions.Entities;

public class MetricOutboxMessage
{
    public long Id { get; set; }

    public long MetricId { get; set; }

    public OutboxMessageStatus Status { get; set; } = OutboxMessageStatus.Pending;

    public int AttemptCount { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? SentAtUtc { get; set; }

    public string? LastError { get; set; }

    public Metric Metric { get; set; } = null!;
}
