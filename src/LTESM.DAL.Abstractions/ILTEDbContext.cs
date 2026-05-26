using LTESM.DAL.Abstractions.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace LTESM.DAL.Abstractions;

public interface ILTEDbContext
{
    DatabaseFacade Database { get; }

    DbSet<Metric> Metrics { get; }
    DbSet<MetricIpAddress> MetricIpAddresses { get; }
    DbSet<MetricDiskSpace> MetricDiskSpaces { get; }
    DbSet<MetricProcess> MetricProcesses { get; }
    DbSet<MetricMonitoredProcess> MetricMonitoredProcesses { get; }
    DbSet<MetricOutboxMessage> MetricOutboxMessages { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
