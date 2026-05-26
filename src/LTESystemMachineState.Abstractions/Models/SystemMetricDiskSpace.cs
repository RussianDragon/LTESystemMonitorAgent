namespace LTESystemMachineState.Abstractions.Models;

public sealed record SystemMetricDiskSpace(
    string Name,
    string? VolumeLabel,
    string? DriveFormat,
    long TotalSpaceBytes,
    long FreeSpaceBytes);
