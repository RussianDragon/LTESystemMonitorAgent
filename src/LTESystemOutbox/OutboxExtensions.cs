using LTESystemOutbox.Abstractions;
using LTESystemOutbox.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LTESystemOutbox;

public static class OutboxExtensions
{
    public static IServiceCollection AddOutbox(
        this IServiceCollection services,
        IConfigurationSection configurationSection)
    {
        var configuration = configurationSection.Get<OutboxConfiguration>()
            ?? throw new InvalidOperationException("Configuration section 'Outbox' is missing or invalid.");

        if (configuration.BatchSize <= 0)
        {
            throw new InvalidOperationException("Configuration setting 'Outbox:BatchSize' must be greater than zero.");
        }

        services.Configure<OutboxConfiguration>(configurationSection);
        services.AddSingleton<MetricPayloadFactory>();
        services.AddScoped<IOutboxDispatcher, OutboxDispatcher>();

        return services;
    }
}
