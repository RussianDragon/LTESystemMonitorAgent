using LTESystemMetricDelivery.Abstractions;
using LTESystemMetricDelivery.Http.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LTESystemMetricDelivery.Http;

public static class HttpMetricDeliveryExtensions
{
    public static IServiceCollection AddHttpMetricDelivery(
        this IServiceCollection services,
        IConfigurationSection configurationSection,
        Action<IHttpClientBuilder>? configureHttpClient = null)
    {
        var configuration = configurationSection.Get<HttpMetricDeliveryConfiguration>()
            ?? throw new InvalidOperationException("Configuration section 'HttpMetricDelivery' is missing or invalid.");

        if (string.IsNullOrWhiteSpace(configuration.ApiUrl))
        {
            throw new InvalidOperationException("Configuration setting 'HttpMetricDelivery:ApiUrl' is required.");
        }

        if (!Uri.TryCreate(configuration.ApiUrl, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException("Configuration setting 'HttpMetricDelivery:ApiUrl' must be an absolute URI.");
        }

        if (configuration.HttpTimeoutSeconds <= 0)
        {
            throw new InvalidOperationException("Configuration setting 'HttpMetricDelivery:HttpTimeoutSeconds' must be greater than zero.");
        }

        services.Configure<HttpMetricDeliveryConfiguration>(configurationSection);
        var httpClientBuilder = services.AddHttpClient<HttpMetricDeliveryClient>(httpClient =>
        {
            httpClient.Timeout = TimeSpan.FromSeconds(configuration.HttpTimeoutSeconds);
        });

        configureHttpClient?.Invoke(httpClientBuilder);

        services.AddScoped<IMetricDeliveryClient>(provider => provider.GetRequiredService<HttpMetricDeliveryClient>());

        return services;
    }
}
