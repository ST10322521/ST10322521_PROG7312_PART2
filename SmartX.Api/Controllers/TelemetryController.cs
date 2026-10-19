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
    private readonly TelemetryHistoryService _historyService;

    public TelemetryController(
        TelemetryQueueService telemetryQueue,
        LiveSensorRegistry sensorRegistry,
        TelemetryHistoryService historyService)
    {
        _telemetryQueue = telemetryQueue;
        _sensorRegistry = sensorRegistry;
        _historyService = historyService;
    }
    /// <summary>
    /// Receives telemetry data from devices and enqueues it for processing. The method accepts a TelemetryRequest object containing the telemetry data, which is then added to the TelemetryQueueService for further handling. The method returns an HTTP 202 Accepted response, indicating that the telemetry data has been successfully queued for processing, along with the current length of the queue.
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
    /// Retrieves the current active alerts, including disconnected devices and active error states. The method queries the LiveSensorRegistry to get a list of devices that have been disconnected for more than 10 seconds and retrieves the current active error states. It returns an HTTP 200 OK response with a JSON object containing the active errors and the list of disconnected devices, allowing clients to monitor the health and status of connected devices in real-time.
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
    /// <summary>
    /// Retrieves the telemetry history for a specific device. The method accepts a deviceId as a parameter and queries the TelemetryHistoryService to get the historical telemetry data associated with that device. If the deviceId is not provided or is empty, it returns a BadRequest response. Otherwise, it returns an HTTP 200 OK response with the telemetry history data, allowing clients to access past telemetry readings for analysis or troubleshooting.
    /// </summary>
    /// <param name="deviceId"></param>
    /// <returns></returns>
    [HttpGet("history/{deviceId}")]
    public IActionResult GetHistory(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return BadRequest(new
            {
                error = "Device ID is required."
            });
        }

        var history = _historyService.GetHistory(deviceId);

        return Ok(history);
    }
}