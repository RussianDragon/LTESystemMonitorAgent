using LTESM.DAL.Abstractions;
using LTESM.DAL.Abstractions.Entities;
using LTESystemMetricDelivery.Abstractions;
using LTESystemOutbox.Abstractions;
using LTESystemOutbox.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LTESystemOutbox;

internal class OutboxDispatcher(
    ILTEDbContext dbContext,
    IMetricDeliveryClient metricDeliveryClient,
    MetricPayloadFactory metricPayloadFactory,
    IOptions<OutboxConfiguration> options,
    ILogger<OutboxDispatcher> logger) : IOutboxDispatcher
{
    public async Task DispatchAsync(CancellationToken cancellationToken = default)
    {
        var configuration = options.Value;
        var batchSize = Math.Max(configuration.BatchSize, 1);

        var messages = await dbContext.MetricOutboxMessages
            .Where(message => message.Status == OutboxMessageStatus.Pending
                || message.Status == OutboxMessageStatus.Failed)
            .OrderBy(message => message.Id)
            .Take(batchSize)
            .Include(message => message.Metric)
                .ThenInclude(metric => metric.IpAddresses)
            .Include(message => message.Metric)
                .ThenInclude(metric => metric.DiskSpaces)
            .Include(message => message.Metric)
                .ThenInclude(metric => metric.RunningProcesses)
            .Include(message => message.Metric)
                .ThenInclude(metric => metric.MonitoredProcesses)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0)
        {
            logger.LogDebug("No outbox messages ready for dispatch.");
            return;
        }

        logger.LogInformation("Dispatching {MessageCount} outbox messages.", messages.Count);

        foreach (var message in messages)
        {
            var dispatchSucceeded = await DispatchMessageAsync(message, cancellationToken);

            if (!dispatchSucceeded)
            {
                logger.LogInformation(
                    "Outbox dispatch stopped after failure on message {OutboxMessageId} to preserve message order.",
                    message.Id);

                break;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> DispatchMessageAsync(
        MetricOutboxMessage message,
        CancellationToken cancellationToken)
    {
        try
        {
            message.AttemptCount++;

            var payload = metricPayloadFactory.Create(message.Metric);
            var deliveryResult = await metricDeliveryClient.SendAsync(payload, cancellationToken);

            if (deliveryResult.Succeeded)
            {
                message.Status = OutboxMessageStatus.Sent;
                message.SentAtUtc = DateTimeOffset.UtcNow;
                message.LastError = null;

                logger.LogInformation(
                    "Outbox message {OutboxMessageId} for metric {MetricId} sent successfully.",
                    message.Id,
                    message.MetricId);

                return true;
            }

            var error = deliveryResult.Error ?? "Metric delivery failed without error details.";
            MarkFailed(message, error);

            logger.LogWarning(
                "Outbox message {OutboxMessageId} for metric {MetricId} dispatch failed: {Error}.",
                message.Id,
                message.MetricId,
                error);

            return false;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            MarkFailed(message, exception.Message);

            logger.LogWarning(
                exception,
                "Outbox message {OutboxMessageId} for metric {MetricId} dispatch failed.",
                message.Id,
                message.MetricId);

            return false;
        }
    }

    private static void MarkFailed(
        MetricOutboxMessage message,
        string error)
    {
        message.Status = OutboxMessageStatus.Failed;
        message.LastError = error;
    }
}
