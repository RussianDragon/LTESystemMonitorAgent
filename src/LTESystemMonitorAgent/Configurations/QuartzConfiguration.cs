namespace LTESystemMonitorAgent.Configurations;

public class QuartzConfiguration
{
    public string MetricCollectionCronExpression { get; set; } = "0/30 * * * * ?";

    public string OutboxDispatchCronExpression { get; set; } = "0/10 * * * * ?";
}
