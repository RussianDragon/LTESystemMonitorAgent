using LTESystemMonitoring.Abstractions;
using Quartz;

namespace LTESystemMonitorAgent.Jobs;

[DisallowConcurrentExecution]
public class CollectMetricsJob(
    IMetricCollectionService metricCollectionService,
    ILogger<CollectMetricsJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("Metric collection job started.");

        await metricCollectionService.CollectAndSaveAsync(context.CancellationToken);

        logger.LogInformation("Metric collection job completed.");
    }
}
