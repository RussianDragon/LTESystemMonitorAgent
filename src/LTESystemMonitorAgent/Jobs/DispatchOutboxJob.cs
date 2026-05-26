using LTESystemOutbox.Abstractions;
using Quartz;

namespace LTESystemMonitorAgent.Jobs;

[DisallowConcurrentExecution]
public class DispatchOutboxJob(
    IOutboxDispatcher outboxDispatcher,
    ILogger<DispatchOutboxJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("Outbox dispatch job started.");

        await outboxDispatcher.DispatchAsync(context.CancellationToken);

        logger.LogInformation("Outbox dispatch job completed.");
    }
}
