using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Services;
using SmartX.Shared.DTOs;

namespace SmartX.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TelemetryController : ControllerBase
{
    private readonly TelemetryQueueService _telemetryQueue;
    private readonly LiveSensorRegistry _sensorRegistry;

    public TelemetryController(TelemetryQueueService telemetryQueue, LiveSensorRegistry sensorRegistry)
    {
        _telemetryQueue = telemetryQueue;
        _sensorRegistry = sensorRegistry;
    }
    /// <summary>
    /// Receives telemetry data from a sensor and enqueues it for processing. The method takes a TelemetryRequest object as input, which contains the device ID, timestamp, telemetry type, and value. It enqueues the request into the TelemetryQueueService for asynchronous processing by the background service. The method returns an HTTP 202 Accepted response with a message indicating that the telemetry has been queued for processing, along with the current length of the queue.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost]
    public IActionResult ReceiveTelemetry(TelemetryRequest request)
    {
        _telemetryQueue.Enqueue(request);

        return Accepted(new
        {
            message = "Telemetry queued for processing.",
            queueLength = _telemetryQueue.Count
        });
    }
    /// <summary>
    /// Retrieves the current active alerts, including disconnected devices and active error states. The method queries the LiveSensorRegistry to get a list of devices that have not sent telemetry data within the last 10 seconds and retrieves any active error states. It returns an HTTP 200 OK response with a JSON object containing the lists of active errors and disconnected devices.
    /// </summary>
    /// <returns></returns>
    [HttpGet("alerts")]
    public IActionResult GetActiveAlerts()
    {
        var disconnected = _sensorRegistry.GetDisconnectedDevices(TimeSpan.FromSeconds(10));
        var activeErrors = _sensorRegistry.GetActiveErrorStates();

        return Ok(new
        {
            activeErrors,
            disconnectedDevices = disconnected
        });
    }
}