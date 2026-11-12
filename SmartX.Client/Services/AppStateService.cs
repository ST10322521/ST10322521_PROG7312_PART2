using SmartX.Client.Models;
using SmartX.Shared.DTOs;
using SmartX.Shared.Enums;

namespace SmartX.Client.Services;

/// <summary>
/// Service to manage the application state, including telemetry data, anomalies, alerts, recommendations, and SignalR connection status.
/// </summary>
public sealed class AppStateService
{
    private readonly Dictionary<string, TelemetryResponse> _latestTelemetry = new();
    private readonly List<TelemetryResponse> _recentAnomalies = new();

    private List<string> _activeErrorStates = [];
    private List<string> _disconnectedDevices = [];
    private List<RecommendationViewModel> _recommendations = [];

    public event Action? StateChanged;

    public IReadOnlyDictionary<string, TelemetryResponse> LatestTelemetry =>
        _latestTelemetry;

    public IReadOnlyList<TelemetryResponse> RecentAnomalies =>
        _recentAnomalies;

    public IReadOnlyList<string> ActiveErrorStates =>
        _activeErrorStates;

    public IReadOnlyList<string> DisconnectedDevices =>
        _disconnectedDevices;

    public IReadOnlyList<RecommendationViewModel> Recommendations =>
        _recommendations;

    public int AnomalyCount { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the SignalR connection is currently connected.
    /// </summary>
    public bool SignalRConnected { get; private set; }

    /// <summary>
    /// Updates the latest telemetry data for a device and manages the list of recent anomalies based on the telemetry status.
    /// </summary>
    /// <param name="telemetry"></param>
    public void UpdateTelemetry(TelemetryResponse telemetry)
    {
        _latestTelemetry[telemetry.DeviceId] = telemetry;

        if (telemetry.Status != TelemetryStatus.Normal)
        {
            AnomalyCount++;

            _recentAnomalies.Insert(0, telemetry);

            if (_recentAnomalies.Count > 20)
            {
                _recentAnomalies.RemoveAt(_recentAnomalies.Count - 1);
            }
        }

        NotifyStateChanged();
    }

    /// <summary>
    /// Updates the active error states and disconnected devices in the application state.
    /// </summary>
    /// <param name="activeErrorStates"></param>
    /// <param name="disconnectedDevices"></param>
    public void UpdateAlerts(
        IEnumerable<string> activeErrorStates,
        IEnumerable<string> disconnectedDevices)
    {
        _activeErrorStates = activeErrorStates.ToList();
        _disconnectedDevices = disconnectedDevices.ToList();

        NotifyStateChanged();
    }

    /// <summary>
    /// Updates the recommendations in the application state.
    /// </summary>
    public void UpdateRecommendations(
        IEnumerable<RecommendationViewModel> recommendations)
    {
        _recommendations = recommendations.ToList();

        NotifyStateChanged();
    }

    /// <summary>
    /// Updates the SignalR connection status in the application state.
    /// </summary>
    /// <param name="connected"></param>
    public void SetSignalRConnected(bool connected)
    {
        if (SignalRConnected == connected)
        {
            return;
        }

        SignalRConnected = connected;
        NotifyStateChanged();
    }

    /// <summary>
    /// Clears the application state, resetting all telemetry, anomalies, alerts, recommendations, and connection status.
    /// </summary>
    public void Clear()
    {
        _latestTelemetry.Clear();
        _recentAnomalies.Clear();

        _activeErrorStates = [];
        _disconnectedDevices = [];
        _recommendations = [];

        AnomalyCount = 0;
        SignalRConnected = false;

        NotifyStateChanged();
    }

    /// <summary>
    /// Notifies subscribers that the application state has changed.
    /// </summary>
    private void NotifyStateChanged()
    {
        StateChanged?.Invoke();
    }
}