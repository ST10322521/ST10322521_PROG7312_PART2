using Microsoft.AspNetCore.SignalR;
using SmartX.Api.Hubs;
using SmartX.Shared.DTOs;

// https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/protected

namespace SmartX.Api.Services;

public sealed class DeviceCommandProcessor : BackgroundService
{
    private readonly DeviceCommandQueueService _queue;
    private IHubContext<SmartXHub> _hubContext;
    private readonly ILogger<DeviceCommandProcessor> _logger;

    public DeviceCommandProcessor(
        DeviceCommandQueueService queue,
        IHubContext<SmartXHub> hubContext,
        ILogger<DeviceCommandProcessor> logger)
    {
        _queue = queue;
        _hubContext = hubContext;
        _logger = logger;
    }
    /// <summary>
    /// Executes the background service that processes device command requests from the queue. The method runs in a loop until the service is stopped, waiting for commands to be available in the queue. When a command is dequeued, it sends the command to all connected clients via SignalR using the SmartXHub. The method handles exceptions and logs any errors that occur during processing. It also logs when the service starts and stops.
    /// </summary>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Smart-X device command processor started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var command = await _queue.DequeueAsync(stoppingToken);

                await _hubContext.Clients.All.SendAsync(
                    "ReceiveDeviceCommand",
                    command,
                    stoppingToken);

                _logger.LogInformation($"Command dispatched: Device={command.DeviceId}, Command={command.Command}");
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing device command.");
            }
        }

        _logger.LogInformation("Smart-X device command processor stropped.");
    }
}

