using LTESystemMetricDelivery.Abstractions;
using LTESystemMetricDelivery.Abstractions.Models;
using LTESystemMetricDelivery.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LTESystemOutbox.Tests;

public class HttpMetricDeliveryClientTests
{
    [Fact(DisplayName = "Тест проверяет, что HTTP-доставка возвращает успех, когда API принял метрику")]
    public async Task SendAsync_WhenApiAcceptsMetric_SendsPostRequestAndReturnsSuccess()
    {
        var httpHandler = new SuccessfulApiHandler();
        await using var provider = CreateServiceProvider(httpHandler);
        var deliveryClient = provider.GetRequiredService<IMetricDeliveryClient>();

        var result = await deliveryClient.SendAsync(CreatePayload());

        Assert.True(result.Succeeded);
        Assert.Null(result.Error);
        Assert.Equal(1, httpHandler.RequestCount);
        Assert.Equal(HttpMethod.Post, httpHandler.LastRequestMethod);
        Assert.Equal(new Uri("https://metrics.example.test/api/metrics"), httpHandler.LastRequestUri);
        Assert.Contains("\"hostname\":\"test-host\"", httpHandler.LastRequestBody);
    }

    [Fact(DisplayName = "Тест проверяет, что HTTP-доставка возвращает ошибку, когда API отвечает неуспешным статусом")]
    public async Task SendAsync_WhenApiReturnsErrorStatus_ReturnsFailureWithResponseDetails()
    {
        await using var provider = CreateServiceProvider(new FailedStatusCodeApiHandler());
        var deliveryClient = provider.GetRequiredService<IMetricDeliveryClient>();

        var result = await deliveryClient.SendAsync(CreatePayload());

        Assert.False(result.Succeeded);
        Assert.Contains("HTTP 500", result.Error);
        Assert.Contains("temporary failure", result.Error);
    }

    [Fact(DisplayName = "Тест проверяет, что HTTP-доставка возвращает ошибку, когда API недоступен")]
    public async Task SendAsync_WhenApiIsUnavailable_ReturnsFailure()
    {
        await using var provider = CreateServiceProvider(new UnavailableApiHandler());
        var deliveryClient = provider.GetRequiredService<IMetricDeliveryClient>();

        var result = await deliveryClient.SendAsync(CreatePayload());

        Assert.False(result.Succeeded);
        Assert.False(string.IsNullOrWhiteSpace(result.Error));
    }

    private static ServiceProvider CreateServiceProvider(HttpMessageHandler httpMessageHandler)
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddHttpMetricDelivery(CreateSection(
            ("ApiUrl", "https://metrics.example.test/api/metrics"),
            ("HttpTimeoutSeconds", "10")), httpClientBuilder =>
        {
            httpClientBuilder.ConfigurePrimaryHttpMessageHandler(() => httpMessageHandler);
        });

        return services.BuildServiceProvider();
    }

    private static MetricPayload CreatePayload()
    {
        return new MetricPayload(
            DateTimeOffset.UtcNow,
            "test-host",
            "test-windows",
            123,
            10,
            20,
            1024,
            512,
            [],
            [],
            [],
            []);
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

    private sealed class UnavailableApiHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            throw new HttpRequestException("API приема метрик недоступен.");
        }
    }

    private sealed class SuccessfulApiHandler : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        public HttpMethod? LastRequestMethod { get; private set; }

        public Uri? LastRequestUri { get; private set; }

        public string LastRequestBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            LastRequestMethod = request.Method;
            LastRequestUri = request.RequestUri;
            LastRequestBody = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("""{"accepted":true}""")
            };
        }
    }

    private sealed class FailedStatusCodeApiHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)
            {
                ReasonPhrase = "Internal Server Error",
                Content = new StringContent("temporary failure")
            });
        }
    }
}
