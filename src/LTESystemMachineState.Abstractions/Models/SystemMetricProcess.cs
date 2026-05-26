namespace LTESystemMachineState.Abstractions.Models;

public sealed record SystemMetricProcess(
    int ProcessId,
    string Name,
    DateTimeOffset? StartedAtUtc,
    long? WorkingSetBytes);
