namespace LTESystemOutbox;

internal sealed record MetricProcessPayload(
    int ProcessId,
    string Name,
    DateTimeOffset? StartedAtUtc,
    long? WorkingSetBytes);
