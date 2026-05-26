namespace LTESystemMetricDelivery.Http.Configurations;

public class HttpMetricDeliveryConfiguration
{
    public required string ApiUrl { get; set; } = string.Empty;

    public int HttpTimeoutSeconds { get; set; } = 10;
}
