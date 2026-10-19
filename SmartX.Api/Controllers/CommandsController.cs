using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Services;
using SmartX.Shared.DTOs;

namespace SmartX.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommandsController : ControllerBase
{
    private readonly DeviceCommandQueueService _commandQueue;

    public CommandsController(DeviceCommandQueueService commandQueue)
    {
        _commandQueue = commandQueue;
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
}