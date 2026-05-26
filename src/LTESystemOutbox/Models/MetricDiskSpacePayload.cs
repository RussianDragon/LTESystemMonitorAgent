namespace LTESystemOutbox;

internal sealed record MetricDiskSpacePayload(
    string Name,
    string? VolumeLabel,
    string? DriveFormat,
    long TotalSpaceBytes,
    long FreeSpaceBytes);
