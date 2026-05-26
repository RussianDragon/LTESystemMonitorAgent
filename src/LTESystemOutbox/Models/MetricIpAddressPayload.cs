namespace LTESystemOutbox;

internal sealed record MetricIpAddressPayload(
    string Address,
    string? AddressFamily,
    string? NetworkInterfaceName);
