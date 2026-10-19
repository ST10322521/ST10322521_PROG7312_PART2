using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Services;
using SmartX.Shared.DTOs;

namespace SmartX.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommandsController : ControllerBase
{
    private readonly DeviceCommandQueueService _commandQueue;
    private readonly DeviceCommandProcessor _commandProcessor;

    public CommandsController(DeviceCommandQueueService commandQueue, DeviceCommandProcessor commandProcessor)
    {
        _commandQueue = commandQueue;
        _commandProcessor = commandProcessor;
    }
    /// <summary>
    /// Receives a device command request and enqueues it for processing. The method validates the incoming request to ensure that the DeviceId and Command are provided and that the command is supported. If the request is valid, it creates a new DeviceCommandRequest object, sets its properties, and enqueues it using the DeviceCommandQueueService. The method returns an HTTP 202 Accepted response with a message indicating that the command has been queued successfully, along with the current length of the queue.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost]
    public IActionResult SendCommand(DeviceCommandRequest request)
    {
        // Validate the request
        if (string.IsNullOrWhiteSpace(request.DeviceId))
        {
            return BadRequest(new { error = "DeviceId is required." });
        }
        if (string.IsNullOrWhiteSpace(request.Command))
        {
            return BadRequest(new { error = "Command is required." });
        }

        var command = request.Command.Trim();

        if (!command.Equals("Start", StringComparison.OrdinalIgnoreCase) &&
           !command.Equals("Stop", StringComparison.OrdinalIgnoreCase) &&
           !command.Equals("SetState", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "Unsupported command." });
        }

        var queuedCommand = new DeviceCommandRequest
        {
            DeviceId = request.DeviceId,
            Command = command,
            Value = request.Value,
            Timestamp = request.Timestamp == default ? DateTime.UtcNow : request.Timestamp
        };

        _commandQueue.Enqueue(queuedCommand);

        return Accepted(new
        {
            message = "Command queued successfully.",
            queueLength = _commandQueue.Count
        });
    }
    /// <summary>
    /// Undoes the last device command that was processed. The method checks if there are any commands in the command history stack. If there are no commands to undo, it returns a 404 Not Found response with a message indicating that there is no command to undo. If there is a command to undo, it pops the last command from the stack, creates an undo command (e.g., if the last command was "Start", it creates a "Stop" command), and sends this undo command to all connected clients via SignalR using the SmartXHub. The method returns an HTTP 200 OK response with a message indicating that the last command has been undone successfully.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost("undo")]
    public async Task<IActionResult> Undo(CancellationToken cancellationToken)
    {
        var undone = await _commandProcessor.UndoLastCommandAsync(cancellationToken);
        if (!undone)
        {
            return NotFound(new { message = "No command to undo." });
        }

        return Ok(new { message = "Last command undone successfully." });
    }
}