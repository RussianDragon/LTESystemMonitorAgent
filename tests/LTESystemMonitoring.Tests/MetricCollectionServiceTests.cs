using LTESM.DAL.Abstractions;
using LTESM.DAL.Abstractions.Entities;
using LTESM.DAL.SQLite;
using LTESM.DAL.SQLite.Configurations;
using LTESystemMachineState.Abstractions;
using LTESystemMachineState.Abstractions.Models;
using LTESystemMonitoring.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LTESystemMonitoring.Tests;

public class MetricCollectionServiceTests
{
    private static readonly DateTimeOffset CollectedAtUtc = new(2026, 05, 26, 10, 20, 30, TimeSpan.Zero);
    private static readonly DateTimeOffset ProcessStartedAtUtc = new(2026, 05, 26, 09, 15, 00, TimeSpan.Zero);

    [Fact(DisplayName = "Тест проверяет, что сбор мониторинга сохраняет основные поля снимка состояния компьютера")]
    public async Task CollectAndSaveAsync_SavesMainMetricFields()
    {
        var result = await CollectMetricAsync(CreateSnapshot());

        Assert.Equal(250, result.SnapshotProvider.LastCpuSampleMilliseconds);
        Assert.Equal(CollectedAtUtc, result.Metric.CollectedAtUtc);
        Assert.Equal("TEST-HOST", result.Metric.Hostname);
        Assert.Equal("Test Windows 11", result.Metric.WindowsVersion);
        Assert.Equal(12345, result.Metric.UptimeSeconds);
        Assert.Equal(12.34, result.Metric.CpuUsagePercent);
        Assert.Equal(56.78, result.Metric.RamUsagePercent);
        Assert.Equal(32768, result.Metric.TotalMemoryBytes);
        Assert.Equal(8192, result.Metric.AvailableMemoryBytes);
    }

    [Fact(DisplayName = "Тест проверяет, что сбор мониторинга сохраняет IP-адреса, диски и процессы отдельными связанными записями")]
    public async Task CollectAndSaveAsync_SavesSnapshotCollections()
    {
        var result = await CollectMetricAsync(CreateSnapshot());

        Assert.Collection(result.Metric.IpAddresses.OrderBy(item => item.Address),
            ipAddress =>
            {
                Assert.Equal("10.0.0.10", ipAddress.Address);
                Assert.Equal("InterNetwork", ipAddress.AddressFamily);
                Assert.Equal("Ethernet 1", ipAddress.NetworkInterfaceName);
            },
            ipAddress =>
            {
                Assert.Equal("fe80::1", ipAddress.Address);
                Assert.Equal("InterNetworkV6", ipAddress.AddressFamily);
                Assert.Equal("Wi-Fi", ipAddress.NetworkInterfaceName);
            });

        Assert.Collection(result.Metric.DiskSpaces.OrderBy(item => item.Name),
            diskSpace =>
            {
                Assert.Equal("C:\\", diskSpace.Name);
                Assert.Equal("System", diskSpace.VolumeLabel);
                Assert.Equal("NTFS", diskSpace.DriveFormat);
                Assert.Equal(512000, diskSpace.TotalSpaceBytes);
                Assert.Equal(128000, diskSpace.FreeSpaceBytes);
            },
            diskSpace =>
            {
                Assert.Equal("D:\\", diskSpace.Name);
                Assert.Equal("Data", diskSpace.VolumeLabel);
                Assert.Equal("exFAT", diskSpace.DriveFormat);
                Assert.Equal(1024000, diskSpace.TotalSpaceBytes);
                Assert.Equal(768000, diskSpace.FreeSpaceBytes);
            });

        Assert.Collection(result.Metric.RunningProcesses.OrderBy(item => item.ProcessId),
            process =>
            {
                Assert.Equal(101, process.ProcessId);
                Assert.Equal("dotnet", process.Name);
                Assert.Equal(ProcessStartedAtUtc, process.StartedAtUtc);
                Assert.Equal(111111, process.WorkingSetBytes);
            },
            process =>
            {
                Assert.Equal(202, process.ProcessId);
                Assert.Equal("worker", process.Name);
                Assert.Null(process.StartedAtUtc);
                Assert.Null(process.WorkingSetBytes);
            });
    }

    [Fact(DisplayName = "Тест проверяет, что после сохранения метрики создается новое сообщение Outbox в статусе Pending")]
    public async Task CollectAndSaveAsync_CreatesPendingOutboxMessage()
    {
        var result = await CollectMetricAsync(CreateSnapshot());

        Assert.NotNull(result.Metric.OutboxMessage);
        Assert.Equal(OutboxMessageStatus.Pending, result.Metric.OutboxMessage.Status);
        Assert.Equal(CollectedAtUtc, result.Metric.OutboxMessage.CreatedAtUtc);
        Assert.Equal(0, result.Metric.OutboxMessage.AttemptCount);
        Assert.Null(result.Metric.OutboxMessage.SentAtUtc);
        Assert.Null(result.Metric.OutboxMessage.LastError);
    }

    [Fact(DisplayName = "Тест проверяет, что отслеживаемые процессы сопоставляются по имени без учета расширения и регистра")]
    public async Task CollectAndSaveAsync_MatchesMonitoredProcessesByNormalizedName()
    {
        var result = await CollectMetricAsync(CreateSnapshot(), new MonitoringConfiguration
        {
            CpuSampleMilliseconds = 250,
            MonitoredProcesses = ["DOTNET.EXE", "worker", "missing-process"]
        });

        var monitoredProcesses = result.Metric.MonitoredProcesses.ToDictionary(
            process => process.Name,
            StringComparer.Ordinal);

        Assert.True(monitoredProcesses["DOTNET.EXE"].IsRunning);
        Assert.Equal(1, monitoredProcesses["DOTNET.EXE"].MatchedProcessCount);

        Assert.True(monitoredProcesses["worker"].IsRunning);
        Assert.Equal(1, monitoredProcesses["worker"].MatchedProcessCount);

        Assert.False(monitoredProcesses["missing-process"].IsRunning);
        Assert.Equal(0, monitoredProcesses["missing-process"].MatchedProcessCount);
    }

    private static async Task<CollectedMetricResult> CollectMetricAsync(
        SystemMetricSnapshot snapshot,
        MonitoringConfiguration? configuration = null)
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"ltesm-monitoring-tests-{Guid.NewGuid():N}.db");
        var snapshotProvider = new FakeSystemMetricSnapshotProvider(snapshot);

        try
        {
            await using var provider = CreateServiceProvider(databasePath);
            await using var scope = provider.CreateAsyncScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<ILTEDbContext>();
            await dbContext.Database.EnsureCreatedAsync();

            var service = new MetricCollectionService(
                dbContext,
                Options.Create(configuration ?? new MonitoringConfiguration
                {
                    CpuSampleMilliseconds = 250,
                    MonitoredProcesses = ["dotnet.exe", "missing-process"]
                }),
                snapshotProvider,
                NullLogger<MetricCollectionService>.Instance);

            await service.CollectAndSaveAsync();

            var metric = await dbContext.Metrics
                .AsNoTracking()
                .Include(item => item.IpAddresses)
                .Include(item => item.DiskSpaces)
                .Include(item => item.RunningProcesses)
                .Include(item => item.MonitoredProcesses)
                .Include(item => item.OutboxMessage)
                .SingleAsync();

            return new CollectedMetricResult(metric, snapshotProvider);
        }
        finally
        {
            DeleteDatabaseFiles(databasePath);
        }
    }

    private static SystemMetricSnapshot CreateSnapshot()
    {
        return new SystemMetricSnapshot(
            CollectedAtUtc,
            "TEST-HOST",
            "Test Windows 11",
            12345,
            12.34,
            56.78,
            32768,
            8192,
            [
                new SystemMetricIpAddress("10.0.0.10", "InterNetwork", "Ethernet 1"),
                new SystemMetricIpAddress("fe80::1", "InterNetworkV6", "Wi-Fi")
            ],
            [
                new SystemMetricDiskSpace("C:\\", "System", "NTFS", 512000, 128000),
                new SystemMetricDiskSpace("D:\\", "Data", "exFAT", 1024000, 768000)
            ],
            [
                new SystemMetricProcess(101, "dotnet", ProcessStartedAtUtc, 111111),
                new SystemMetricProcess(202, "worker", null, null)
            ]);
    }

    private static ServiceProvider CreateServiceProvider(string databasePath)
    {
        var services = new ServiceCollection();

        services.AddSQLiteDbContext(new DatabaseConfiguration
        {
            ConnectionString = $"Data Source={databasePath};Pooling=False",
            LoggingEnabled = false
        });

        return services.BuildServiceProvider();
    }

    private static void DeleteDatabaseFiles(string databasePath)
    {
        foreach (var path in new[] { databasePath, $"{databasePath}-shm", $"{databasePath}-wal" })
        {
            DeleteFileIfExists(path);
        }
    }

    private static void DeleteFileIfExists(string path)
    {
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                return;
            }
            catch (IOException) when (attempt < 3)
            {
                Thread.Sleep(100);
            }
        }
    }

    private sealed record CollectedMetricResult(
        Metric Metric,
        FakeSystemMetricSnapshotProvider SnapshotProvider);

    private sealed class FakeSystemMetricSnapshotProvider(
        SystemMetricSnapshot snapshot) : ISystemMetricSnapshotProvider
    {
        public int? LastCpuSampleMilliseconds { get; private set; }

        public Task<SystemMetricSnapshot> CollectAsync(
            int cpuSampleMilliseconds,
            CancellationToken cancellationToken = default)
        {
            LastCpuSampleMilliseconds = cpuSampleMilliseconds;

            return Task.FromResult(snapshot);
        }
    }
}
