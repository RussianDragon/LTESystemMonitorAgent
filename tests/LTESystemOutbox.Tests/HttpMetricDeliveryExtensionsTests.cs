using LTESystemMetricDelivery.Abstractions;
using LTESystemMetricDelivery.Http;
using LTESystemMetricDelivery.Http.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LTESystemOutbox.Tests;

public class HttpMetricDeliveryExtensionsTests
{
    [Fact(DisplayName = "Тест проверяет, что корректная конфигурация HTTP-доставки регистрирует отправитель метрик в DI")]
    public void AddHttpMetricDelivery_WithValidConfiguration_RegistersDeliveryClientAndOptions()
    {
        var services = new ServiceCollection();

        services.AddHttpMetricDelivery(CreateSection(
            ("ApiUrl", "https://localhost:7200/api/metrics"),
            ("HttpTimeoutSeconds", "15")));

        var descriptor = Assert.Single(services, service => service.ServiceType == typeof(IMetricDeliveryClient));
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);

        using var provider = services.BuildServiceProvider();
        var configuration = provider.GetRequiredService<IOptions<HttpMetricDeliveryConfiguration>>().Value;

        Assert.Equal("https://localhost:7200/api/metrics", configuration.ApiUrl);
        Assert.Equal(15, configuration.HttpTimeoutSeconds);
    }

    [Theory(DisplayName = "Тест проверяет, что адрес API для HTTP-доставки метрик обязателен")]
    [InlineData("")]
    [InlineData(" ")]
    public void AddHttpMetricDelivery_WhenApiUrlIsEmpty_Throws(string apiUrl)
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddHttpMetricDelivery(CreateSection(("ApiUrl", apiUrl))));

        Assert.Contains("HttpMetricDelivery:ApiUrl", exception.Message);
    }

    [Fact(DisplayName = "Тест проверяет, что адрес API для HTTP-доставки метрик должен быть абсолютным URI")]
    public void AddHttpMetricDelivery_WhenApiUrlIsRelative_Throws()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddHttpMetricDelivery(CreateSection(("ApiUrl", "/api/metrics"))));

        Assert.Contains("HttpMetricDelivery:ApiUrl", exception.Message);
    }

    [Theory(DisplayName = "Тест проверяет, что HTTP timeout для доставки метрик должен быть больше нуля")]
    [InlineData("0")]
    [InlineData("-1")]
    public void AddHttpMetricDelivery_WhenHttpTimeoutSecondsIsNotPositive_Throws(string timeoutSeconds)
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddHttpMetricDelivery(CreateSection(
                ("ApiUrl", "https://localhost:7200/api/metrics"),
                ("HttpTimeoutSeconds", timeoutSeconds))));

        Assert.Contains("HttpMetricDelivery:HttpTimeoutSeconds", exception.Message);
    }

    private static IConfigurationSection CreateSection(params (string Key, string? Value)[] settings)
    {
        var values = settings.ToDictionary(
            setting => $"HttpMetricDelivery:{setting.Key}",
            setting => setting.Value);

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build()
            .GetSection("HttpMetricDelivery");
    }
}
