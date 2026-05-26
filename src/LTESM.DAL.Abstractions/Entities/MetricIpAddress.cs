namespace LTESM.DAL.Abstractions.Entities;

public class MetricIpAddress
{
    public long Id { get; set; }

    public long MetricId { get; set; }

    public required string Address { get; set; } = string.Empty;

    public string? AddressFamily { get; set; }

    public string? NetworkInterfaceName { get; set; }

    public Metric Metric { get; set; } = null!;
}
