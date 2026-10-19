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

    public void Dispose()
    {
        SignalRService.TelemetryReceived -= HandleTelemetry;
    }
}
