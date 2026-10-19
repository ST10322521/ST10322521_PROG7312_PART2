using SmartX.Shared.DTOs;

// https://learn.microsoft.com/en-us/dotnet/api/system.threading.semaphoreslim?view=net-10.0
// https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/get

namespace SmartX.Api.Services;

public sealed class DeviceCommandQueueService
{
    private readonly Queue<DeviceCommandRequest> _queue = new();
    private readonly SemaphoreSlim _signal = new(0);
    /// <summary>
    /// Gets the count of device command requests currently in the queue. The property uses a lock to ensure thread safety when accessing the queue. It returns the number of commands waiting to be processed.
    /// </summary>
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
    /// <summary>
    /// Enqueues a device command request into the queue. The method takes a DeviceCommandRequest object as input, which contains the device ID, command, value, and timestamp. It locks the queue to ensure thread safety while adding the command to the queue. After adding the command, it releases the semaphore to signal that a new command is available for processing.
    /// </summary>
    /// <param name="command"></param>
    public void Enqueue(DeviceCommandRequest command)
    {
        lock (_queue)
        {
            _queue.Enqueue(command);
        }

        _signal.Release();
    }
    /// <summary>
    /// Dequeues a device command request from the queue. The method waits asynchronously for a command to be available in the queue, using a SemaphoreSlim to signal when a command is added. Once a command is available, it locks the queue, dequeues the command, and returns it. The method takes a CancellationToken as a parameter to allow for cancellation of the wait operation.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<DeviceCommandRequest> DequeueAsync(CancellationToken cancellationToken)
    {
        await _signal.WaitAsync(cancellationToken);

        lock (_queue)
        {
            return _queue.Dequeue();
        }
    }
}