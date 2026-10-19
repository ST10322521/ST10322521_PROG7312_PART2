using Microsoft.AspNetCore.Components;
using MudBlazor;
using SmartX.Client.Services;
using SmartX.Shared.DTOs;
using SmartX.Shared.Enums;
using SmartX.Shared.Models;

namespace SmartX.Client.Pages;

public partial class Dashboard : IDisposable
{
    [Inject]
    private SensorService SensorService { get; set; } = null!;

    [Inject]
    private SignalRService SignalRService { get; set; } = null!;

    private List<Sensor> sensors = new();

    private readonly Dictionary<string, TelemetryResponse> latestTelemetry = new();

    private readonly List<TelemetryResponse> recentAnomalies = new();

    private int anomalyCount;

    private bool signalRConnected;

    private string? error;

    protected override async Task OnInitializedAsync()
    {
        SignalRService.TelemetryReceived += HandleTelemetry;

        signalRConnected =
            SignalRService.State ==
            Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Connected;

        try
        {
            sensors = await SensorService.GetSensorsAsync();
        }
        catch (Exception ex)
        {
            error = $"Unable to load dashboard sensors: {ex.Message}";
        }
    }
    /// <summary>
    /// Handles incoming telemetry data received from the SignalR service. The method updates the latest telemetry readings for each device, increments the anomaly count if the telemetry status is not normal, and maintains a list of recent anomalies. It also checks the connection state of the SignalR service and triggers a UI update by invoking StateHasChanged asynchronously.
    /// </summary>
    /// <param name="telemetry"></param>
    private void HandleTelemetry(TelemetryResponse telemetry)
    {

        Console.WriteLine(
       $"[DASHBOARD] Device={telemetry.DeviceId} Value={telemetry.Value} Delta={telemetry.Delta}");

        latestTelemetry[telemetry.DeviceId] = telemetry;

        if (telemetry.Status != TelemetryStatus.Normal)
        {
            anomalyCount++;

            recentAnomalies.Insert(0, telemetry);

            if (recentAnomalies.Count > 20)
            {
                recentAnomalies.RemoveAt(recentAnomalies.Count - 1);
            }
        }

        signalRConnected =
            SignalRService.State ==
            Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Connected;

        InvokeAsync(StateHasChanged);
    }
    /// <summary>
    /// Gets the color associated with a specific telemetry status. The method maps different telemetry statuses (Normal, Warning, Critical) to corresponding MudBlazor color values (Success, Warning, Error). If the status is not recognized, it defaults to the default color. This is used to visually represent the status of telemetry readings in the UI.
    /// </summary>
    /// <param name="status"></param>
    /// <returns></returns>
    private static Color GetStatusColor(TelemetryStatus status)
    {
        return status switch
        {
            TelemetryStatus.Normal => Color.Success,
            TelemetryStatus.Warning => Color.Warning,
            TelemetryStatus.Critical => Color.Error,
            _ => Color.Default
        };
    }
    /// <summary>
    /// Disposes of the Dashboard component by unsubscribing from the TelemetryReceived event of the SignalR service. This ensures that when the component is no longer in use, it stops receiving telemetry updates, preventing potential memory leaks or unintended behavior.
    /// </summary>
    public void Dispose()
    {
        SignalRService.TelemetryReceived -= HandleTelemetry;
    }
}
