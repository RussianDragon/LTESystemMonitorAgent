namespace LTESystemMetricDelivery.Abstractions.Models;

public sealed record MetricDiskSpacePayload(
    string Name,
    string? VolumeLabel,
    string? DriveFormat,
    long TotalSpaceBytes,
    long FreeSpaceBytes);
