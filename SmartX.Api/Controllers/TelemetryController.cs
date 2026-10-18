using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Services;

namespace SmartX.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TelemetryController : ControllerBase
{
    private readonly TelemetryQueueService _telemetryQueue;

    public TelemetryController(
        TelemetryQueueService telemetryQueue)
    {
        _telemetryQueue = telemetryQueue;
    }
    /// <summary>
    /// Receives telemetry data from a sensor and enqueues it for processing. The method takes a TelemetryRequest object as input, which contains the device ID, timestamp, telemetry type, and value. It enqueues the request into the TelemetryQueueService for asynchronous processing by the background service. The method returns an HTTP 202 Accepted response with a message indicating that the telemetry has been queued for processing, along with the current length of the queue.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost]
    public IActionResult ReceiveTelemetry(
        SmartX.Shared.DTOs.TelemetryRequest request)
    {
        _telemetryQueue.Enqueue(request);

        return Accepted(new
        {
            message = "Telemetry queued for processing.",
            queueLength = _telemetryQueue.Count
        });
    }
}