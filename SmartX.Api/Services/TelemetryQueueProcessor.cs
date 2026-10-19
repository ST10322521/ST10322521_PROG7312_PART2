using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;
using SmartX.Api.Hubs;
using SmartX.Api.Services.Interfaces;
using SmartX.Shared.DTOs;
using SmartX.Shared.Enums;

namespace SmartX.Api.Services;

public sealed class TelemetryQueueProcessor : BackgroundService
{
    private readonly TelemetryQueueService _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<SmartXHub> _hubContext;
    private readonly ILogger<TelemetryQueueProcessor> _logger;

    private readonly PriorityQueue<TelemetryResponse, int> _criticalQueue = new();

    private readonly object _criticalQueueLock = new();
    private readonly LiveSensorRegistry _sensorRegistry;

    public TelemetryQueueProcessor(
        TelemetryQueueService queue,
        IServiceScopeFactory scopeFactory,
        IHubContext<SmartXHub> hubContext,
        ILogger<TelemetryQueueProcessor> logger,
        LiveSensorRegistry sensorRegistry)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
        _logger = logger;
        _sensorRegistry = sensorRegistry;
    }
    /// <summary>
    /// Executes the telemetry queue processing in a background service. The method continuously dequeues telemetry requests from the TelemetryQueueService and processes them using the ITelemetryIngestionService. If the telemetry response indicates a critical status, it is enqueued into a priority queue for critical alerts. The method also handles exceptions and logs relevant information. The processing continues until the service is stopped or canceled.
    /// </summary>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Smart-X telemetry FIFO processor started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var request = await _queue.DequeueAsync(stoppingToken);

                TelemetryResponse? response;

                using (var scope = _scopeFactory.CreateScope())
                {
                    var ingestionService = scope.ServiceProvider.GetRequiredService<ITelemetryIngestionService>();

                    response = await ingestionService.ReceiveTelemetryAsync(request);
                }

                if (response is null)
                {
                    _logger.LogWarning(
                        "Telemetry ignored because sensor {DeviceId} is not registered.",
                        request.DeviceId);

                    continue;
                }

                if (response.Status == TelemetryStatus.Critical)
                {
                    EnqueueCritical(response);
                    await ProcessCriticalAlertsAsync(stoppingToken);
                }
                else
                {
                    await PublishTelemetryAsync(response, stoppingToken);
                }
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing queued telemetry.");
            }
        }

        _logger.LogInformation("Smart-X telemetry FIFO processor stopped.");
    }
    /// <summary>
    /// Enqueues critical telemetry responses into a priority queue for processing. The method takes a TelemetryResponse object as input and adds it to the _criticalQueue with a priority of 0 (highest priority). It also logs a warning message indicating that critical telemetry has been queued for processing, along with the device ID associated with the response.
    /// </summary>
    /// <param name="response"></param>
    private void EnqueueCritical(TelemetryResponse response)
    {
        lock (_criticalQueueLock)
        {
            _criticalQueue.Enqueue(response, 0);
        }

        _logger.LogWarning(
            "Critical telemetry queued with priority: Device={DeviceId}",
            response.DeviceId);
    }
    /// <summary>
    /// Processes critical telemetry alerts from the priority queue asynchronously. The method continuously dequeues critical telemetry responses from the _criticalQueue and publishes them to all connected clients via SignalR using the PublishTelemetryAsync method. The processing continues until the queue is empty or the cancellation token is triggered.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task ProcessCriticalAlertsAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            TelemetryResponse? response;

            lock (_criticalQueueLock)
            {
                if (!_criticalQueue.TryDequeue(
                        out response,
                        out _))
                {
                    break;
                }
            }

            await PublishTelemetryAsync(
                response,
                cancellationToken);
        }
    }
    /// <summary>
    /// Publishes telemetry responses to all connected clients via SignalR asynchronously. The method takes a TelemetryResponse object and a CancellationToken as input. It uses the IHubContext to send the "TelemetryReceived" message to all clients, along with the telemetry response data. This allows clients to receive real-time updates on telemetry data, including critical alerts.
    /// </summary>
    /// <param name="response"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task PublishTelemetryAsync(TelemetryResponse response, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.All.SendAsync("TelemetryReceived", response, cancellationToken);
    }
}