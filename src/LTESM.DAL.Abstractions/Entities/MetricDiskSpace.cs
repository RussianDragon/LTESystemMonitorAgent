namespace LTESM.DAL.Abstractions.Entities;

public class MetricDiskSpace
{
    public long Id { get; set; }

    public long MetricId { get; set; }

    public required string Name { get; set; } = string.Empty;

    public string? VolumeLabel { get; set; }

    public string? DriveFormat { get; set; }

    public long TotalSpaceBytes { get; set; }

    public long FreeSpaceBytes { get; set; }

    public Metric Metric { get; set; } = null!;
}
