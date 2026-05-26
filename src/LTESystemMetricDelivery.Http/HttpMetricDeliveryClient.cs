using System.Net.Http.Json;
using LTESystemMetricDelivery.Abstractions;
using LTESystemMetricDelivery.Abstractions.Models;
using LTESystemMetricDelivery.Http.Configurations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LTESystemMetricDelivery.Http;

internal class HttpMetricDeliveryClient(
    HttpClient httpClient,
    IOptions<HttpMetricDeliveryConfiguration> options,
    ILogger<HttpMetricDeliveryClient> logger) : IMetricDeliveryClient
{
    public async Task<MetricDeliveryResult> SendAsync(
        MetricPayload payload,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var configuration = options.Value;
            using var response = await httpClient.PostAsJsonAsync(
                configuration.ApiUrl,
                payload,
                cancellationToken);

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return MetricDeliveryResult.Success();
            }

            return MetricDeliveryResult.Failure(
                $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}. Response: {responseBody}");
        }
        catch (HttpRequestException exception)
        {
            return CreateFailureResult(exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            return CreateFailureResult(exception);
        }
    }

    private MetricDeliveryResult CreateFailureResult(Exception exception)
    {
        logger.LogWarning(exception, "HTTP metric delivery failed.");
        return MetricDeliveryResult.Failure(exception.Message);
    }
}
