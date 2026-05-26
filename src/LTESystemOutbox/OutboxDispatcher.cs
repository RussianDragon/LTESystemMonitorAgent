using System.Net.Http.Json;
using LTESM.DAL.Abstractions;
using LTESM.DAL.Abstractions.Entities;
using LTESystemOutbox.Abstractions;
using LTESystemOutbox.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LTESystemOutbox;

internal class OutboxDispatcher(
    ILTEDbContext dbContext,
    HttpClient httpClient,
    IOptions<SystemOutboxConfiguration> options,
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
            var dispatchSucceeded = await DispatchMessageAsync(message, configuration, cancellationToken);

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
        SystemOutboxConfiguration configuration,
        CancellationToken cancellationToken)
    {
        try
        {
            message.AttemptCount++;

            var payload = CreatePayload(message.Metric);
            using var response = await httpClient.PostAsJsonAsync(configuration.ApiUrl, payload, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
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

            var error = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}. Response: {responseBody}";
            MarkFailed(message, error);

            logger.LogWarning(
                "Outbox message {OutboxMessageId} for metric {MetricId} dispatch failed: {Error}.",
                message.Id,
                message.MetricId,
                error);

            return false;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
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

    private static MetricPayload CreatePayload(Metric metric)
    {
        return new MetricPayload(
            metric.CollectedAtUtc,
            metric.Hostname,
            metric.WindowsVersion,
            metric.UptimeSeconds,
            metric.CpuUsagePercent,
            metric.RamUsagePercent,
            metric.TotalMemoryBytes,
            metric.AvailableMemoryBytes,
            metric.IpAddresses.Select(ipAddress => new MetricIpAddressPayload(
                ipAddress.Address,
                ipAddress.AddressFamily,
                ipAddress.NetworkInterfaceName)).ToArray(),
            metric.DiskSpaces.Select(diskSpace => new MetricDiskSpacePayload(
                diskSpace.Name,
                diskSpace.VolumeLabel,
                diskSpace.DriveFormat,
                diskSpace.TotalSpaceBytes,
                diskSpace.FreeSpaceBytes)).ToArray(),
            metric.RunningProcesses.Select(process => new MetricProcessPayload(
                process.ProcessId,
                process.Name,
                process.StartedAtUtc,
                process.WorkingSetBytes)).ToArray(),
            metric.MonitoredProcesses.Select(process => new MetricMonitoredProcessPayload(
                process.Name,
                process.IsRunning,
                process.MatchedProcessCount)).ToArray());
    }

    private static void MarkFailed(
        MetricOutboxMessage message,
        string error)
    {
        message.Status = OutboxMessageStatus.Failed;
        message.LastError = error;
    }
}
