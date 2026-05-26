using LTESM.DAL.Abstractions;
using LTESM.DAL.Abstractions.Entities;
using LTESM.DAL.SQLite;
using LTESM.DAL.SQLite.Configurations;
using LTESystemMetricDelivery.Abstractions;
using LTESystemMetricDelivery.Abstractions.Models;
using LTESystemOutbox.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LTESystemOutbox.Tests;

public class OutboxDispatcherTests
{
    [Fact(DisplayName = "Тест проверяет, что при ошибке доставки Outbox не падает и оставляет сообщение для повторной отправки")]
    public async Task DispatchAsync_WhenDeliveryFails_MarksMessageAsFailedAndDoesNotThrow()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"ltesm-outbox-tests-{Guid.NewGuid():N}.db");
        var deliveryClient = new UnavailableMetricDeliveryClient();

        try
        {
            await using (var provider = CreateServiceProvider(databasePath, deliveryClient))
            {
                await CreateDatabaseAndSeedPendingMessageAsync(provider);

                await using (var scope = provider.CreateAsyncScope())
                {
                    var dispatcher = scope.ServiceProvider.GetRequiredService<IOutboxDispatcher>();

                    var exception = await Record.ExceptionAsync(() => dispatcher.DispatchAsync());

                    Assert.Null(exception);
                }

                await using (var scope = provider.CreateAsyncScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ILTEDbContext>();
                    var message = await dbContext.MetricOutboxMessages.AsNoTracking().SingleAsync();

                    Assert.Equal(OutboxMessageStatus.Failed, message.Status);
                    Assert.Equal(1, message.AttemptCount);
                    Assert.Null(message.SentAtUtc);
                    Assert.False(string.IsNullOrWhiteSpace(message.LastError));
                }
            }

            Assert.Equal(1, deliveryClient.SendCount);
        }
        finally
        {
            DeleteDatabaseFiles(databasePath);
        }
    }

    [Fact(DisplayName = "Тест проверяет, что успешная доставка Outbox помечает сообщение отправленным")]
    public async Task DispatchAsync_WhenDeliveryAcceptsMessage_MarksMessageAsSent()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"ltesm-outbox-tests-{Guid.NewGuid():N}.db");
        var deliveryClient = new SuccessfulMetricDeliveryClient();

        try
        {
            await using (var provider = CreateServiceProvider(databasePath, deliveryClient))
            {
                await CreateDatabaseAndSeedPendingMessageAsync(provider);

                await using (var scope = provider.CreateAsyncScope())
                {
                    var dispatcher = scope.ServiceProvider.GetRequiredService<IOutboxDispatcher>();

                    await dispatcher.DispatchAsync();
                }

                await using (var scope = provider.CreateAsyncScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ILTEDbContext>();
                    var message = await dbContext.MetricOutboxMessages.AsNoTracking().SingleAsync();

                    Assert.Equal(OutboxMessageStatus.Sent, message.Status);
                    Assert.Equal(1, message.AttemptCount);
                    Assert.NotNull(message.SentAtUtc);
                    Assert.Null(message.LastError);
                }

                Assert.Equal(1, deliveryClient.SendCount);
            }
        }
        finally
        {
            DeleteDatabaseFiles(databasePath);
        }
    }

    [Fact(DisplayName = "Тест проверяет, что уже отправленное сообщение Outbox не отправляется повторно")]
    public async Task DispatchAsync_WhenMessageIsAlreadySent_DoesNotSendAgain()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"ltesm-outbox-tests-{Guid.NewGuid():N}.db");
        var deliveryClient = new SuccessfulMetricDeliveryClient();

        try
        {
            await using (var provider = CreateServiceProvider(databasePath, deliveryClient))
            {
                await CreateDatabaseAndSeedMessageAsync(
                    provider,
                    OutboxMessageStatus.Sent,
                    attemptCount: 1,
                    sentAtUtc: new DateTimeOffset(2026, 05, 26, 11, 31, 00, TimeSpan.Zero));

                await using (var scope = provider.CreateAsyncScope())
                {
                    var dispatcher = scope.ServiceProvider.GetRequiredService<IOutboxDispatcher>();

                    await dispatcher.DispatchAsync();
                }

                await using (var scope = provider.CreateAsyncScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ILTEDbContext>();
                    var message = await dbContext.MetricOutboxMessages.AsNoTracking().SingleAsync();

                    Assert.Equal(OutboxMessageStatus.Sent, message.Status);
                    Assert.Equal(1, message.AttemptCount);
                    Assert.NotNull(message.SentAtUtc);
                }

                Assert.Equal(0, deliveryClient.SendCount);
            }
        }
        finally
        {
            DeleteDatabaseFiles(databasePath);
        }
    }

    [Fact(DisplayName = "Тест проверяет, что сообщение Outbox после ошибки доставки повторно отправляется при следующем запуске диспетчера")]
    public async Task DispatchAsync_WhenFailedMessageIsRetriedAndDeliveryAcceptsMessage_MarksMessageAsSent()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"ltesm-outbox-tests-{Guid.NewGuid():N}.db");
        var deliveryClient = new FailOnceThenSuccessMetricDeliveryClient();

        try
        {
            await using (var provider = CreateServiceProvider(databasePath, deliveryClient))
            {
                await CreateDatabaseAndSeedPendingMessageAsync(provider);

                await using (var scope = provider.CreateAsyncScope())
                {
                    var dispatcher = scope.ServiceProvider.GetRequiredService<IOutboxDispatcher>();

                    await dispatcher.DispatchAsync();
                }

                await using (var scope = provider.CreateAsyncScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ILTEDbContext>();
                    var message = await dbContext.MetricOutboxMessages.AsNoTracking().SingleAsync();

                    Assert.Equal(OutboxMessageStatus.Failed, message.Status);
                    Assert.Equal(1, message.AttemptCount);
                    Assert.Null(message.SentAtUtc);
                    Assert.False(string.IsNullOrWhiteSpace(message.LastError));
                }

                await using (var scope = provider.CreateAsyncScope())
                {
                    var dispatcher = scope.ServiceProvider.GetRequiredService<IOutboxDispatcher>();

                    await dispatcher.DispatchAsync();
                }

                await using (var scope = provider.CreateAsyncScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ILTEDbContext>();
                    var message = await dbContext.MetricOutboxMessages.AsNoTracking().SingleAsync();

                    Assert.Equal(OutboxMessageStatus.Sent, message.Status);
                    Assert.Equal(2, message.AttemptCount);
                    Assert.NotNull(message.SentAtUtc);
                    Assert.Null(message.LastError);
                }

                Assert.Equal(2, deliveryClient.SendCount);
            }
        }
        finally
        {
            DeleteDatabaseFiles(databasePath);
        }
    }

    private static ServiceProvider CreateServiceProvider(
        string databasePath,
        IMetricDeliveryClient metricDeliveryClient)
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddSingleton(metricDeliveryClient);
        services.AddSQLiteDbContext(new DatabaseConfiguration
        {
            ConnectionString = $"Data Source={databasePath};Pooling=False",
            LoggingEnabled = false
        });

        services.AddOutbox(CreateOutboxSection(
            ("BatchSize", "1")));

        return services.BuildServiceProvider();
    }

    private static async Task CreateDatabaseAndSeedPendingMessageAsync(ServiceProvider provider)
    {
        await CreateDatabaseAndSeedMessageAsync(provider, OutboxMessageStatus.Pending);
    }

    private static async Task CreateDatabaseAndSeedMessageAsync(
        ServiceProvider provider,
        OutboxMessageStatus status,
        int attemptCount = 0,
        DateTimeOffset? sentAtUtc = null)
    {
        await using var scope = provider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ILTEDbContext>();

        await dbContext.Database.EnsureCreatedAsync();

        dbContext.Metrics.Add(new Metric
        {
            CollectedAtUtc = new DateTimeOffset(2026, 05, 26, 11, 30, 00, TimeSpan.Zero),
            Hostname = "test-host",
            WindowsVersion = "test-windows",
            UptimeSeconds = 123,
            CpuUsagePercent = 10,
            RamUsagePercent = 20,
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
            ],
            OutboxMessage = new MetricOutboxMessage
            {
                CreatedAtUtc = new DateTimeOffset(2026, 05, 26, 11, 30, 00, TimeSpan.Zero),
                Status = status,
                AttemptCount = attemptCount,
                SentAtUtc = sentAtUtc
            }
        });

        await dbContext.SaveChangesAsync();
    }

    private static IConfigurationSection CreateOutboxSection(params (string Key, string? Value)[] settings)
    {
        var values = settings.ToDictionary(
            setting => $"Outbox:{setting.Key}",
            setting => setting.Value);

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build()
            .GetSection("Outbox");
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

    private abstract class FakeMetricDeliveryClient : IMetricDeliveryClient
    {
        public List<MetricPayload> SentPayloads { get; } = [];

        public int SendCount => SentPayloads.Count;

        public Task<MetricDeliveryResult> SendAsync(
            MetricPayload payload,
            CancellationToken cancellationToken = default)
        {
            SentPayloads.Add(payload);

            return Task.FromResult(GetResult(SendCount));
        }

        protected abstract MetricDeliveryResult GetResult(int sendCount);
    }

    private sealed class UnavailableMetricDeliveryClient : FakeMetricDeliveryClient
    {
        protected override MetricDeliveryResult GetResult(int sendCount)
        {
            return MetricDeliveryResult.Failure("Канал доставки метрик недоступен.");
        }
    }

    private sealed class SuccessfulMetricDeliveryClient : FakeMetricDeliveryClient
    {
        protected override MetricDeliveryResult GetResult(int sendCount)
        {
            return MetricDeliveryResult.Success();
        }
    }

    private sealed class FailOnceThenSuccessMetricDeliveryClient : FakeMetricDeliveryClient
    {
        protected override MetricDeliveryResult GetResult(int sendCount)
        {
            if (sendCount == 1)
            {
                return MetricDeliveryResult.Failure("Канал доставки метрик недоступен.");
            }

            return MetricDeliveryResult.Success();
        }
    }
}
