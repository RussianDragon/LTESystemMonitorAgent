namespace LTESystemOutbox;

internal sealed record MetricMonitoredProcessPayload(
    string Name,
    bool IsRunning,
    int MatchedProcessCount);
