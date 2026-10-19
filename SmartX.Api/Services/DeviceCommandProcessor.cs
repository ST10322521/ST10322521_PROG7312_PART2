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
    private readonly Stack<DeviceCommandRequest> _commandHistory = new();

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

                _commandHistory.Push(command);

                await _hubContext.Clients.All.SendAsync(
                    "DeviceCommandReceived",
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
    public async Task<bool> UndoLastCommandAsync(CancellationToken cancellationToken = default)
    {
        if (_commandHistory.Count is 0)
        {
            return false;
        }

        var lastCommand = _commandHistory.Pop();
        var undoCommand = new DeviceCommandRequest
        {
            DeviceId = lastCommand.DeviceId,
            Command = lastCommand.Command.Equals("Start", StringComparison.OrdinalIgnoreCase) ? "Stop" : "Start",
            Value = null,
            Timestamp = DateTime.UtcNow
        };

        await _hubContext.Clients.All.SendAsync(
            "DeviceCommandReceived",
            undoCommand,
            cancellationToken);

        _logger.LogInformation($"Undo command dispatched: Device={undoCommand.DeviceId}, Command={undoCommand.Command}");

        return true;
    }
}

