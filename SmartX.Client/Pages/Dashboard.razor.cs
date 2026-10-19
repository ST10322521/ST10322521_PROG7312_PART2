using Microsoft.AspNetCore.Components;
using MudBlazor;
using SmartX.Client.Models;
using SmartX.Client.Services;
using SmartX.Shared.DTOs;
using SmartX.Shared.Enums;
using SmartX.Shared.Models;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;
using System.Text.Json;

namespace SmartX.Client.Pages;

public partial class Dashboard : IDisposable
{
    [Inject]
    private SensorService SensorService { get; set; } = null!;
    [Inject]
    private SignalRService SignalRService { get; set; } = null!;
    [Inject]
    private HttpClient Http { get; set; } = null!;

    private List<Sensor> sensors = new();
    private readonly Dictionary<string, TelemetryResponse> latestTelemetry = new();
    private readonly List<TelemetryResponse> recentAnomalies = new();
    private int anomalyCount;
    private bool signalRConnected;
    private string? error;
    private List<string> activeErrorStates = new();
    private List<string> disconnectedDevices = new();

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
        Console.WriteLine($"[DASHBOARD] Device={telemetry.DeviceId} Value={telemetry.Value} Delta={telemetry.Delta}");

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

        signalRConnected = SignalRService.State == HubConnectionState.Connected;

        _ = RefreshAlertsAsync();
    }
    /// <summary>
    /// Asynchronously refreshes the active error alerts by calling the LoadActiveErrorAlertsAsync method and then triggers a UI update by invoking StateHasChanged. This ensures that the dashboard reflects the latest state of active errors and disconnected devices.
    /// </summary>
    /// <returns></returns>
    private async Task RefreshAlertsAsync()
    {
        await LoadActiveErrorAlertsAsync();
        await InvokeAsync(StateHasChanged);
    }
    /// <summary>
    /// Asynchronously loads the active error alerts from the server. The method sends an HTTP GET request to the "api/alerts/active" endpoint and expects a response containing lists of active errors and disconnected devices. If the request is successful, it updates the local lists of active error states and disconnected devices. In case of an exception during the request, it logs the error message to the console.
    /// </summary>
    /// <returns></returns>
    private async Task LoadActiveErrorAlertsAsync()
    {
        try
        {
            var result = await Http.GetFromJsonAsync<ActiveAlertsResponse>("api/Telemetry/alerts", new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            if (result != null)
            {
                activeErrorStates = result.ActiveErrors.ToList();
                disconnectedDevices = result.DisconnectedDevices.ToList();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DASHBOARD] Error loading active error alerts: {ex.Message}");
        }
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
