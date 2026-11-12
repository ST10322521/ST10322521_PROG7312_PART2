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
    private AppStateService AppState { get; set; } = null!;

    [Inject]
    private HttpClient Http { get; set; } = null!;

    private List<Sensor> sensors = new();
    private string? error;
    private bool recommendationActionRunning;

    /// <summary>
    /// Initializes the component, subscribes to telemetry and state change events, and loads initial data for sensors, active error alerts, and recommendations.
    /// </summary>
    /// <returns></returns>
    protected override async Task OnInitializedAsync()
    {
        SignalRService.TelemetryReceived += HandleTelemetry;
        AppState.StateChanged += HandleStateChanged;

        AppState.SetSignalRConnected(SignalRService.State == HubConnectionState.Connected);

        try
        {
            sensors = await SensorService.GetSensorsAsync();

            await LoadActiveErrorAlertsAsync();
            await LoadRecommendationsAsync();
        }
        catch (Exception ex)
        {
            error = $"Unable to load dashboard sensors: {ex.Message}";
        }
    }

    /// <summary>
    /// Handles incoming telemetry data, updates the application state, and sets the SignalR connection status.
    /// </summary>
    /// <param name="telemetry"></param>
    private void HandleTelemetry(TelemetryResponse telemetry)
    {
        Console.WriteLine(
            $"[DASHBOARD] Device={telemetry.DeviceId} " +
            $"Value={telemetry.Value} Delta={telemetry.Delta}");

        AppState.UpdateTelemetry(telemetry);

        AppState.SetSignalRConnected(SignalRService.State == HubConnectionState.Connected);
    }

    /// <summary>
    /// Handles state changes in the application state service and triggers a UI update.
    /// </summary>
    private void HandleStateChanged()
    {
        _ = InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Loads the active error alerts from the API and updates the application state with the retrieved data. If an error occurs during the API call, it logs the error message to the console.
    /// </summary>
    /// <returns></returns>
    private async Task LoadActiveErrorAlertsAsync()
    {
        try
        {
            var result =
                await Http.GetFromJsonAsync<ActiveAlertsResponse>(
                    "api/Telemetry/alerts",
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (result != null)
            {
                AppState.UpdateAlerts(result.ActiveErrors, result.DisconnectedDevices);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DASHBOARD] Error loading active error alerts: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the color associated with a given telemetry status. Returns a MudBlazor Color based on the status value.
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
    /// Loads the recommendations from the API and updates the application state with the retrieved data. If an error occurs during the API call, it logs the error message to the console.
    /// </summary>
    /// <returns></returns>
    private async Task LoadRecommendationsAsync()
    {
        try
        {
            var result =
                await Http.GetFromJsonAsync<List<RecommendationViewModel>>(
                    "api/recommendations",
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            AppState.UpdateRecommendations(
                result ?? []);
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"[DASHBOARD] Error loading recommendations: {ex.Message}");
        }
    }

    /// <summary>
    /// Executes the suggested command for a given recommendation. If the command is not "Inspect" and the device ID is valid, it sends a command request to the API. After executing the command, it reloads the recommendations. Any exceptions during the process are logged to the console.
    /// </summary>
    /// <param name="recommendation"></param>
    /// <returns></returns>
    private async Task ExecuteRecommendationAsync(
        RecommendationViewModel recommendation)
    {
        if (recommendation.SuggestedCommand.Equals(
                "Inspect",
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(recommendation.DeviceId))
        {
            return;
        }

        recommendationActionRunning = true;

        try
        {
            var request = new DeviceCommandRequest
            {
                DeviceId = recommendation.DeviceId,
                Command = recommendation.SuggestedCommand,
                Value = null,
                Timestamp = DateTime.UtcNow
            };

            var response =
                await Http.PostAsJsonAsync("api/commands", request);

            if (response.IsSuccessStatusCode)
            {
                await LoadRecommendationsAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"[DASHBOARD] Recommendation action failed: {ex.Message}");
        }
        finally
        {
            recommendationActionRunning = false;
        }
    }

    /// <summary>
    /// Disposes of the component, unsubscribing from telemetry and state change events to prevent memory leaks.
    /// </summary>
    public void Dispose()
    {
        SignalRService.TelemetryReceived -= HandleTelemetry;
        AppState.StateChanged -= HandleStateChanged;
    }
}