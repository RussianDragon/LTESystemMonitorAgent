namespace LTESystemMetricDelivery.Abstractions.Models;

public sealed record MetricMonitoredProcessPayload(
    string Name,
    bool IsRunning,
    int MatchedProcessCount);
