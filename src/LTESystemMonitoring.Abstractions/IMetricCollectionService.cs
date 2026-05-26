namespace LTESystemMonitoring.Abstractions;

public interface IMetricCollectionService
{
    Task CollectAndSaveAsync(CancellationToken cancellationToken = default);
}
