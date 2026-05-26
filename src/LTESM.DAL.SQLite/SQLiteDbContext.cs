using LTESM.DAL.Abstractions;
using LTESM.DAL.Abstractions.Entities;
using Microsoft.EntityFrameworkCore;

namespace LTESM.DAL.SQLite;

internal class SQLiteDbContext(DbContextOptions<SQLiteDbContext> options) : DbContext(options), ILTEDbContext
{
    private const string StringType = "text";
    private const string IntegerType = "integer";
    private const string RealType = "real";

    public DbSet<Metric> Metrics => Set<Metric>();

    public DbSet<MetricIpAddress> MetricIpAddresses => Set<MetricIpAddress>();

    public DbSet<MetricDiskSpace> MetricDiskSpaces => Set<MetricDiskSpace>();

    public DbSet<MetricProcess> MetricProcesses => Set<MetricProcess>();

    public DbSet<MetricMonitoredProcess> MetricMonitoredProcesses => Set<MetricMonitoredProcess>();

    public DbSet<MetricOutboxMessage> MetricOutboxMessages => Set<MetricOutboxMessage>();

    private static void ConfigureMetric(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Metric>()
                                 .ToTable("metric");

        entity.HasKey(e => e.Id);

        entity.Property(e => e.Id).HasColumnName("id").HasColumnType(IntegerType);
        entity.Property(e => e.CollectedAtUtc).HasColumnName("collected_at_utc").HasColumnType(StringType).IsRequired();
        entity.Property(e => e.Hostname).HasColumnName("hostname").HasColumnType(StringType).IsRequired();
        entity.Property(e => e.WindowsVersion).HasColumnName("windows_version").HasColumnType(StringType).IsRequired();
        entity.Property(e => e.UptimeSeconds).HasColumnName("uptime_seconds").HasColumnType(IntegerType).IsRequired();
        entity.Property(e => e.CpuUsagePercent).HasColumnName("cpu_usage_percent").HasColumnType(RealType).IsRequired();
        entity.Property(e => e.RamUsagePercent).HasColumnName("ram_usage_percent").HasColumnType(RealType).IsRequired();
        entity.Property(e => e.TotalMemoryBytes).HasColumnName("total_memory_bytes").HasColumnType(IntegerType).IsRequired();
        entity.Property(e => e.AvailableMemoryBytes).HasColumnName("available_memory_bytes").HasColumnType(IntegerType).IsRequired();

        entity.HasMany(e => e.IpAddresses)
              .WithOne(e => e.Metric)
              .HasForeignKey(e => e.MetricId)
              .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(e => e.DiskSpaces)
              .WithOne(e => e.Metric)
              .HasForeignKey(e => e.MetricId)
              .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(e => e.RunningProcesses)
              .WithOne(e => e.Metric)
              .HasForeignKey(e => e.MetricId)
              .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(e => e.MonitoredProcesses)
              .WithOne(e => e.Metric)
              .HasForeignKey(e => e.MetricId)
              .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(e => e.OutboxMessage)
              .WithOne(e => e.Metric)
              .HasForeignKey<MetricOutboxMessage>(e => e.MetricId)
              .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureMetricIpAddress(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<MetricIpAddress>()
                                 .ToTable("metric_ip_address");

        entity.HasKey(e => e.Id);

        entity.Property(e => e.Id).HasColumnName("id").HasColumnType(IntegerType);
        entity.Property(e => e.MetricId).HasColumnName("metric_id").HasColumnType(IntegerType).IsRequired();
        entity.Property(e => e.Address).HasColumnName("address").HasColumnType(StringType).IsRequired();
        entity.Property(e => e.AddressFamily).HasColumnName("address_family").HasColumnType(StringType);
        entity.Property(e => e.NetworkInterfaceName).HasColumnName("network_interface_name").HasColumnType(StringType);

        entity.HasIndex(e => e.MetricId);
    }

    private static void ConfigureMetricDiskSpace(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<MetricDiskSpace>()
                                 .ToTable("metric_disk_space");

        entity.HasKey(e => e.Id);

        entity.Property(e => e.Id).HasColumnName("id").HasColumnType(IntegerType);
        entity.Property(e => e.MetricId).HasColumnName("metric_id").HasColumnType(IntegerType).IsRequired();
        entity.Property(e => e.Name).HasColumnName("name").HasColumnType(StringType).IsRequired();
        entity.Property(e => e.VolumeLabel).HasColumnName("volume_label").HasColumnType(StringType);
        entity.Property(e => e.DriveFormat).HasColumnName("drive_format").HasColumnType(StringType);
        entity.Property(e => e.TotalSpaceBytes).HasColumnName("total_space_bytes").HasColumnType(IntegerType).IsRequired();
        entity.Property(e => e.FreeSpaceBytes).HasColumnName("free_space_bytes").HasColumnType(IntegerType).IsRequired();

        entity.HasIndex(e => e.MetricId);
    }

    private static void ConfigureMetricProcess(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<MetricProcess>()
                                 .ToTable("metric_process");

        entity.HasKey(e => e.Id);

        entity.Property(e => e.Id).HasColumnName("id").HasColumnType(IntegerType);
        entity.Property(e => e.MetricId).HasColumnName("metric_id").HasColumnType(IntegerType).IsRequired();
        entity.Property(e => e.ProcessId).HasColumnName("process_id").HasColumnType(IntegerType).IsRequired();
        entity.Property(e => e.Name).HasColumnName("name").HasColumnType(StringType).IsRequired();
        entity.Property(e => e.StartedAtUtc).HasColumnName("started_at_utc").HasColumnType(StringType);
        entity.Property(e => e.WorkingSetBytes).HasColumnName("working_set_bytes").HasColumnType(IntegerType);

        entity.HasIndex(e => e.MetricId);
    }

    private static void ConfigureMetricMonitoredProcess(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<MetricMonitoredProcess>()
                                 .ToTable("metric_monitored_process");

        entity.HasKey(e => e.Id);

        entity.Property(e => e.Id).HasColumnName("id").HasColumnType(IntegerType);
        entity.Property(e => e.MetricId).HasColumnName("metric_id").HasColumnType(IntegerType).IsRequired();
        entity.Property(e => e.Name).HasColumnName("name").HasColumnType(StringType).IsRequired();
        entity.Property(e => e.IsRunning).HasColumnName("is_running").HasColumnType(IntegerType).IsRequired();
        entity.Property(e => e.MatchedProcessCount).HasColumnName("matched_process_count").HasColumnType(IntegerType).IsRequired();

        entity.HasIndex(e => e.MetricId);
    }

    private static void ConfigureMetricOutboxMessage(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<MetricOutboxMessage>()
                                 .ToTable("metric_outbox_message");

        entity.HasKey(e => e.Id);

        entity.Property(e => e.Id).HasColumnName("id").HasColumnType(IntegerType);
        entity.Property(e => e.MetricId).HasColumnName("metric_id").HasColumnType(IntegerType).IsRequired();
        entity.Property(e => e.Status).HasColumnName("status").HasColumnType(IntegerType).HasConversion<int>().IsRequired();
        entity.Property(e => e.AttemptCount).HasColumnName("attempt_count").HasColumnType(IntegerType).IsRequired();
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType(StringType).IsRequired();
        entity.Property(e => e.SentAtUtc).HasColumnName("sent_at_utc").HasColumnType(StringType);
        entity.Property(e => e.LastError).HasColumnName("last_error").HasColumnType(StringType);

        entity.HasIndex(e => e.MetricId).IsUnique();
        entity.HasIndex(e => new { e.Status, e.Id });
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureMetric(modelBuilder);
        ConfigureMetricIpAddress(modelBuilder);
        ConfigureMetricDiskSpace(modelBuilder);
        ConfigureMetricProcess(modelBuilder);
        ConfigureMetricMonitoredProcess(modelBuilder);
        ConfigureMetricOutboxMessage(modelBuilder);
    }
}
