using LTESystemMonitoring;
using LTESystemMonitoring.Abstractions;
using LTESystemMonitoring.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LTESystemMonitoring.Tests;

public class SystemMonitoringExtensionsTests
{
    [Fact(DisplayName = "Тест проверяет, что корректная конфигурация Monitoring регистрирует сборщик метрик в DI")]
    public void AddMonitoring_WithValidConfiguration_RegistersCollectorAndOptions()
    {
        var services = new ServiceCollection();

        services.AddMonitoring(CreateSection(
            ("CpuSampleMilliseconds", "250"),
            ("MonitoredProcesses:0", "notepad"),
            ("MonitoredProcesses:1", "dotnet")));

        var descriptor = Assert.Single(services, service => service.ServiceType == typeof(IMetricCollectionService));
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        Assert.Equal(typeof(MetricCollectionService), descriptor.ImplementationType);

        using var provider = services.BuildServiceProvider();
        var configuration = provider.GetRequiredService<IOptions<MonitoringConfiguration>>().Value;

        Assert.Equal(250, configuration.CpuSampleMilliseconds);
        Assert.Equal(["notepad", "dotnet"], configuration.MonitoredProcesses);
    }

    [Theory(DisplayName = "Тест проверяет, что интервал замера CPU должен быть больше нуля")]
    [InlineData("0")]
    [InlineData("-1")]
    public void AddMonitoring_WhenCpuSampleMillisecondsIsNotPositive_Throws(string sampleMilliseconds)
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddMonitoring(CreateSection(("CpuSampleMilliseconds", sampleMilliseconds))));

        Assert.Contains("Monitoring:CpuSampleMilliseconds", exception.Message);
    }

    [Theory(DisplayName = "Тест проверяет, что список отслеживаемых процессов не должен содержать пустые имена")]
    [InlineData("")]
    [InlineData(" ")]
    public void AddMonitoring_WhenMonitoredProcessNameIsEmpty_Throws(string processName)
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddMonitoring(CreateSection(
                ("CpuSampleMilliseconds", "500"),
                ("MonitoredProcesses:0", "notepad"),
                ("MonitoredProcesses:1", processName))));

        Assert.Contains("Monitoring:MonitoredProcesses", exception.Message);
    }

    private static IConfigurationSection CreateSection(params (string Key, string? Value)[] settings)
    {
        var values = settings.ToDictionary(
            setting => $"Monitoring:{setting.Key}",
            setting => setting.Value);

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build()
            .GetSection("Monitoring");
    }
}
