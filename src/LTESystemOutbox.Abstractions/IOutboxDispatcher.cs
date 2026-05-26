namespace LTESystemOutbox.Abstractions;

public interface IOutboxDispatcher
{
    Task DispatchAsync(CancellationToken cancellationToken = default);
}
