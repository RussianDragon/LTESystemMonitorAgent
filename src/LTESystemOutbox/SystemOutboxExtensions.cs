using LTESystemOutbox.Abstractions;
using LTESystemOutbox.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LTESystemOutbox;

public static class SystemOutboxExtensions
{
    public static IServiceCollection AddSystemOutbox(
        this IServiceCollection services,
        IConfigurationSection configurationSection)
    {
        var configuration = configurationSection.Get<SystemOutboxConfiguration>()
            ?? throw new InvalidOperationException("Configuration section 'Outbox' is missing or invalid.");

        if (string.IsNullOrWhiteSpace(configuration.ApiUrl))
        {
            throw new InvalidOperationException("Configuration setting 'Outbox:ApiUrl' is required.");
        }

        if (!Uri.TryCreate(configuration.ApiUrl, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException("Configuration setting 'Outbox:ApiUrl' must be an absolute URI.");
        }

        if (configuration.HttpTimeoutSeconds <= 0)
        {
            throw new InvalidOperationException("Configuration setting 'Outbox:HttpTimeoutSeconds' must be greater than zero.");
        }

        if (configuration.BatchSize <= 0)
        {
            throw new InvalidOperationException("Configuration setting 'Outbox:BatchSize' must be greater than zero.");
        }

        services.Configure<SystemOutboxConfiguration>(configurationSection);
        services.AddHttpClient<OutboxDispatcher>(httpClient =>
        {
            httpClient.Timeout = TimeSpan.FromSeconds(configuration.HttpTimeoutSeconds);
        });

        services.AddScoped<IOutboxDispatcher>(provider => provider.GetRequiredService<OutboxDispatcher>());

        return services;
    }
}
