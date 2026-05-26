using LTESM.DAL.Abstractions.Entities;

namespace LTESystemOutbox.Tests;

public class MetricPayloadFactoryTests
{
    [Fact(DisplayName = "Тест проверяет, что фабрика payload переносит все поля метрики из БД в сообщение доставки")]
    public void Create_MapsMetricFieldsAndCollections()
    {
        var metric = new Metric
        {
            CollectedAtUtc = new DateTimeOffset(2026, 05, 26, 11, 30, 00, TimeSpan.Zero),
            Hostname = "test-host",
            WindowsVersion = "test-windows",
            UptimeSeconds = 123,
            CpuUsagePercent = 10.5,
            RamUsagePercent = 20.5,
            TotalMemoryBytes = 1024,
            AvailableMemoryBytes = 512,
            IpAddresses =
            [
                new MetricIpAddress
                {
                    Address = "10.0.0.10",
                    AddressFamily = "InterNetwork",
                    NetworkInterfaceName = "Ethernet"
                }
            ],
            DiskSpaces =
            [
                new MetricDiskSpace
                {
                    Name = "C:\\",
                    VolumeLabel = "System",
                    DriveFormat = "NTFS",
                    TotalSpaceBytes = 102400,
                    FreeSpaceBytes = 51200
                }
            ],
            RunningProcesses =
            [
                new MetricProcess
                {
                    ProcessId = 101,
                    Name = "dotnet",
                    StartedAtUtc = new DateTimeOffset(2026, 05, 26, 11, 00, 00, TimeSpan.Zero),
                    WorkingSetBytes = 123456
                }
            ],
            MonitoredProcesses =
            [
                new MetricMonitoredProcess
                {
                    Name = "dotnet",
                    IsRunning = true,
                    MatchedProcessCount = 1
                }
            ]
        };

        var payload = new MetricPayloadFactory().Create(metric);

        Assert.Equal(metric.CollectedAtUtc, payload.CollectedAtUtc);
        Assert.Equal("test-host", payload.Hostname);
        Assert.Equal("test-windows", payload.WindowsVersion);
        Assert.Equal(123, payload.UptimeSeconds);
        Assert.Equal(10.5, payload.CpuUsagePercent);
        Assert.Equal(20.5, payload.RamUsagePercent);
        Assert.Equal(1024, payload.TotalMemoryBytes);
        Assert.Equal(512, payload.AvailableMemoryBytes);

        var ipAddress = Assert.Single(payload.IpAddresses);
        Assert.Equal("10.0.0.10", ipAddress.Address);
        Assert.Equal("InterNetwork", ipAddress.AddressFamily);
        Assert.Equal("Ethernet", ipAddress.NetworkInterfaceName);

        var diskSpace = Assert.Single(payload.DiskSpaces);
        Assert.Equal("C:\\", diskSpace.Name);
        Assert.Equal("System", diskSpace.VolumeLabel);
        Assert.Equal("NTFS", diskSpace.DriveFormat);
        Assert.Equal(102400, diskSpace.TotalSpaceBytes);
        Assert.Equal(51200, diskSpace.FreeSpaceBytes);

        var runningProcess = Assert.Single(payload.RunningProcesses);
        Assert.Equal(101, runningProcess.ProcessId);
        Assert.Equal("dotnet", runningProcess.Name);
        Assert.Equal(new DateTimeOffset(2026, 05, 26, 11, 00, 00, TimeSpan.Zero), runningProcess.StartedAtUtc);
        Assert.Equal(123456, runningProcess.WorkingSetBytes);

        var monitoredProcess = Assert.Single(payload.MonitoredProcesses);
        Assert.Equal("dotnet", monitoredProcess.Name);
        Assert.True(monitoredProcess.IsRunning);
        Assert.Equal(1, monitoredProcess.MatchedProcessCount);
    }
}
