namespace LTESystemMetricDelivery.Abstractions.Models;

public sealed record MetricProcessPayload(
    int ProcessId,
    string Name,
    DateTimeOffset? StartedAtUtc,
    long? WorkingSetBytes);
