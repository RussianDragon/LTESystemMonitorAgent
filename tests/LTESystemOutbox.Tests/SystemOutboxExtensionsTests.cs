using LTESystemOutbox.Abstractions;
using LTESystemOutbox.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LTESystemOutbox.Tests;

public class SystemOutboxExtensionsTests
{
    [Fact(DisplayName = "Тест проверяет, что корректная конфигурация Outbox регистрирует диспетчер отправки в DI")]
    public void AddOutbox_WithValidConfiguration_RegistersDispatcherAndOptions()
    {
        var services = new ServiceCollection();

        services.AddOutbox(CreateSection(
            ("BatchSize", "25")));

        var descriptor = Assert.Single(services, service => service.ServiceType == typeof(IOutboxDispatcher));
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);

        using var provider = services.BuildServiceProvider();
        var configuration = provider.GetRequiredService<IOptions<OutboxConfiguration>>().Value;

        Assert.Equal(25, configuration.BatchSize);
    }

    [Theory(DisplayName = "Тест проверяет, что размер пачки Outbox должен быть больше нуля")]
    [InlineData("0")]
    [InlineData("-1")]
    public void AddOutbox_WhenBatchSizeIsNotPositive_Throws(string batchSize)
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddOutbox(CreateSection(("BatchSize", batchSize))));

        Assert.Contains("Outbox:BatchSize", exception.Message);
    }

    private static IConfigurationSection CreateSection(params (string Key, string? Value)[] settings)
    {
        var values = settings.ToDictionary(
            setting => $"Outbox:{setting.Key}",
            setting => setting.Value);

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build()
            .GetSection("Outbox");
    }
}
