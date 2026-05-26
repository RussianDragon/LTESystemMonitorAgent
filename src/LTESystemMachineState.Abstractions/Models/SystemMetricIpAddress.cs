namespace LTESystemMachineState.Abstractions.Models;

public sealed record SystemMetricIpAddress(
    string Address,
    string? AddressFamily,
    string? NetworkInterfaceName);
