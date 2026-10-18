using SmartX.Shared.DTOs;

namespace SmartX.Api.Services;

public class TelemetryQueueService
{
    private readonly Queue<TelemetryRequest> _queue = new();
    private readonly SemaphoreSlim _signal = new(0);

    public int Count
    {
        get
        {
            lock (_queue)
            {
                return _queue.Count;
            }
        }
    }

    public void Enqueue(TelemetryRequest request)
    {
        lock (_queue)
        {
            _queue.Enqueue(request);
        }

        _signal.Release();
    }

    public async Task<TelemetryRequest> DequeueAsync(
        CancellationToken cancellationToken)
    {
        await _signal.WaitAsync(cancellationToken);

        lock (_queue)
        {
            return _queue.Dequeue();
        }
    }
}