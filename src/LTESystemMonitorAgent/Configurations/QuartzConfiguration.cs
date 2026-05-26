namespace LTESystemMonitorAgent.Configurations;

public class QuartzConfiguration
{
    public int MetricCollectionIntervalSeconds { get; set; } = 30;

    public int OutboxDispatchIntervalSeconds { get; set; } = 10;
}
