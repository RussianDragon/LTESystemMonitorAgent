namespace LTESystemMetricDelivery.Abstractions;

public sealed record MetricDeliveryResult(bool Succeeded, string? Error)
{
    public static MetricDeliveryResult Success()
    {
        return new MetricDeliveryResult(true, null);
    }

    public static MetricDeliveryResult Failure(string error)
    {
        return new MetricDeliveryResult(false, error);
    }
}
