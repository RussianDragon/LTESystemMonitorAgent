using LTESystemMetricDelivery.Abstractions.Models;

namespace LTESystemMetricDelivery.Abstractions;

public interface IMetricDeliveryClient
{
    Task<MetricDeliveryResult> SendAsync(
        MetricPayload payload,
        CancellationToken cancellationToken = default);
}
