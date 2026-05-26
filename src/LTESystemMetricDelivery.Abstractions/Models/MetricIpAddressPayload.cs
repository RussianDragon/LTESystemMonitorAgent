namespace LTESystemMetricDelivery.Abstractions.Models;

public sealed record MetricIpAddressPayload(
    string Address,
    string? AddressFamily,
    string? NetworkInterfaceName);
