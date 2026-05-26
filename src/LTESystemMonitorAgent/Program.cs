using LTESM.DAL.Abstractions;
using LTESM.DAL.SQLite;
using LTESM.DAL.SQLite.Configurations;
using LTESystemMachineState;
using LTESystemMetricDelivery.Http;
using LTESystemMonitorAgent.Configurations;
using LTESystemMonitorAgent.Jobs;
using LTESystemMonitoring;
using LTESystemOutbox;
using Microsoft.EntityFrameworkCore;
using NLog;
using NLog.Extensions.Logging;
using Quartz;

Directory.SetCurrentDirectory(AppContext.BaseDirectory);
var builder = Host.CreateApplicationBuilder(args);

var nlogConfigPath = Path.Combine(AppContext.BaseDirectory, "nlog.config");
var bootstrapLogger = LogManager.Setup()
    .SetupExtensions(extensions => extensions.RegisterConfigSettings(builder.Configuration))
    .LoadConfigurationFromFile(nlogConfigPath)
    .GetCurrentClassLogger();

try
{
    bootstrapLogger.Info("Starting LTESystemMonitorAgent.");
    bootstrapLogger.Info("Application logs will be written to {LogFilePath}.",
        builder.Configuration["Logging:FilePath"] ?? "logs/agent.log");

    builder.Logging.ClearProviders();
    builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
    builder.Logging.AddNLog();

    #region Настройка Windows Service
    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = "LTESystemMonitorAgent";
    });
    #endregion

    #region Настройка DAL
    var databaseConfiguration = builder.Configuration
        .GetSection("Database")
        .Get<DatabaseConfiguration>()
        ?? throw new InvalidOperationException("Configuration section 'Database' is missing or invalid.");

    ValidateRequiredString(databaseConfiguration.ConnectionString, "Database:ConnectionString");

    builder.Services.AddSQLiteDbContext(databaseConfiguration);
    #endregion

    builder.Services.AddSystemMachineState();
    builder.Services.AddMonitoring(builder.Configuration.GetSection("Monitoring"));
    builder.Services.AddHttpMetricDelivery(builder.Configuration.GetSection("HttpMetricDelivery"));
    builder.Services.AddOutbox(builder.Configuration.GetSection("Outbox"));

    #region Настройка Quartz
    var quartzConfiguration = builder.Configuration
        .GetSection("Quartz")
        .Get<QuartzConfiguration>()
        ?? throw new InvalidOperationException("Configuration section 'Quartz' is missing or invalid.");

    var metricCollectionIntervalSeconds = GetPositiveIntervalSeconds(
        quartzConfiguration.MetricCollectionIntervalSeconds,
        "Quartz:MetricCollectionIntervalSeconds");

    var outboxDispatchIntervalSeconds = GetPositiveIntervalSeconds(
        quartzConfiguration.OutboxDispatchIntervalSeconds,
        "Quartz:OutboxDispatchIntervalSeconds");

    builder.Services.AddQuartz(quartz =>
    {
        var collectMetricsJobKey = new JobKey("collect-metrics");
        quartz.AddJob<CollectMetricsJob>(job => job.WithIdentity(collectMetricsJobKey));
        quartz.AddTrigger(trigger => trigger
            .ForJob(collectMetricsJobKey)
            .WithIdentity("collect-metrics-trigger")
            .StartAt(DateBuilder.FutureDate(1, IntervalUnit.Second))
            .WithSimpleSchedule(schedule => schedule
                .WithIntervalInSeconds(metricCollectionIntervalSeconds)
                .RepeatForever()));

        var dispatchOutboxJobKey = new JobKey("dispatch-outbox");
        quartz.AddJob<DispatchOutboxJob>(job => job.WithIdentity(dispatchOutboxJobKey));
        quartz.AddTrigger(trigger => trigger
            .ForJob(dispatchOutboxJobKey)
            .WithIdentity("dispatch-outbox-trigger")
            .StartAt(DateBuilder.FutureDate(1, IntervalUnit.Second))
            .WithSimpleSchedule(schedule => schedule
                .WithIntervalInSeconds(outboxDispatchIntervalSeconds)
                .RepeatForever()));
    });

    builder.Services.AddQuartzHostedService(options =>
    {
        options.WaitForJobsToComplete = true;
    });
    #endregion

    var host = builder.Build();
    var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

    lifetime.ApplicationStopping.Register(() =>
    {
        bootstrapLogger.Info("LTESystemMonitorAgent is stopping.");
    });

    lifetime.ApplicationStopped.Register(() =>
    {
        bootstrapLogger.Info("LTESystemMonitorAgent stopped.");
    });

    bootstrapLogger.Info("LTESystemMonitorAgent host built successfully.");

    #region Apply migration
    using var serviceScope = host.Services.CreateScope();

    var dbContext = serviceScope.ServiceProvider.GetRequiredService<ILTEDbContext>();
    var pendingMigrations = dbContext.Database.GetPendingMigrations().ToArray();

    if (pendingMigrations.Length == 0)
    {
        bootstrapLogger.Info("No pending database migrations found.");
    }
    else
    {
        bootstrapLogger.Info("Applying {MigrationCount} database migrations: {Migrations}.",
            pendingMigrations.Length,
            string.Join(", ", pendingMigrations));

        dbContext.Database.Migrate();

        bootstrapLogger.Info("Database migrations applied successfully.");
    }
    #endregion

    host.Run();
}
catch (HostAbortedException)
{
    throw;
}
catch (Exception exception) when (IsConfigurationException(exception))
{
    bootstrapLogger.Fatal(exception, "LTESystemMonitorAgent configuration error: {Error}", exception.Message);
    throw;
}
catch (Exception exception)
{
    bootstrapLogger.Fatal(exception, "LTESystemMonitorAgent terminated unexpectedly.");
    throw;
}
finally
{
    LogManager.Shutdown();
}

static int GetPositiveIntervalSeconds(int intervalSeconds, string settingName)
{
    if (intervalSeconds <= 0)
    {
        throw new InvalidOperationException($"Configuration setting '{settingName}' must be greater than zero.");
    }

    return intervalSeconds;
}

static void ValidateRequiredString(string? value, string settingName)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        throw new InvalidOperationException($"Configuration setting '{settingName}' is required.");
    }
}

static bool IsConfigurationException(Exception exception)
{
    return exception is InvalidOperationException
        && exception.Message.StartsWith("Configuration ", StringComparison.Ordinal);
}
