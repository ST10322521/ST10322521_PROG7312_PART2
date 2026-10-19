using SmartX.Api.Models;
using SmartX.Shared.Models;

namespace SmartX.Api.Services;

public sealed class RecommendationService
{
    private readonly LiveSensorRegistry _sensorRegistry;
    private readonly TelemetryHistoryService _historyService;

    private readonly List<UserInteraction> _interactions = [];
    private readonly object _lock = new();

    public RecommendationService(
        LiveSensorRegistry sensorRegistry,
        TelemetryHistoryService historyService)
    {
        _sensorRegistry = sensorRegistry;
        _historyService = historyService;
    }
    /// <summary>
    /// Records a user interaction with the system. This method is thread-safe and ensures that the list of interactions does not exceed 500 entries. If the list exceeds this limit, the oldest entry is removed. The method takes a UserInteraction object as a parameter, which contains details about the action performed by the user, such as the action type, query, device ID, command, and timestamp.
    /// </summary>
    /// <param name="interaction"></param>
    public void RecordInteraction(UserInteraction interaction)
    {
        lock (_lock)
        {
            _interactions.Add(interaction);

            // Ensure the list does not exceed 500 entries
            if (_interactions.Count > 500)
            {
                _interactions.RemoveAt(0);
            }
        }
    }
    /// <summary>
    /// Retrieves a list of user interactions, ordered by timestamp in descending order. The method is thread-safe and returns a read-only list of UserInteraction objects. It locks the internal list of interactions to ensure that no other thread can modify it while the data is being retrieved. The returned list contains the most recent interactions first, allowing for easy access to the latest user actions.
    /// </summary>
    /// <returns></returns>
    public IReadOnlyList<UserInteraction> GetInteractions()
    {
        lock (_lock)
        {
            return _interactions.OrderByDescending(x => x.Timestamp).ToList();
        }
    }
    /// <summary>
    /// Retrieves the count of "Search" actions performed by the user for a specific device. The method is thread-safe and locks the internal list of interactions to ensure that no other thread can modify it while the count is being calculated. It filters the interactions to include only those that match the specified device ID and have an action type of "Search", then returns the total count of such interactions.
    /// </summary>
    /// <param name="deviceId"></param>
    /// <returns></returns>
    public int GetSearchCount(string deviceId)
    {
        lock (_lock)
        {
            return _interactions.Count(x =>
                string.Equals(x.DeviceId, deviceId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.Action, "Search", StringComparison.OrdinalIgnoreCase));
        }
    }
    /// <summary>
    /// Generates a list of recommendations based on telemetry patterns observed in the recent history of devices. The method iterates through all sensors in the registry, retrieves their telemetry history, and checks for abnormal readings that fall outside the defined normal range. If a device has a significant number of abnormal readings in its recent history, a recommendation is created to inspect the device. The recommendations are returned as a read-only list of Recommendation objects, each containing details about the device, the reason for the recommendation, and suggested actions.
    /// </summary>
    /// <returns></returns>
    private IReadOnlyList<Recommendation> GetTelemetryPatternRecommendations()
    {
        var recommendations = new List<Recommendation>();

        foreach (var sensor in _sensorRegistry.GetAllSensors())
        {
            var history = _historyService.GetHistory(sensor.DeviceId);

            if (history.Count < 3) continue;

            var recentReadings = history
                .OrderByDescending(x => x.Timestamp)
                .Take(5)
                .ToList();

            var abnormalReadings = recentReadings.Count(reading =>
                reading.Value < sensor.NormalMin || reading.Value > sensor.NormalMax);

            if (abnormalReadings < 3) continue;

            recommendations.Add(new Recommendation(
                sensor.DeviceId,
                "Repeated telemetry anomaly",
                $"Recent telemetry history shows {abnormalReadings} of the last {recentReadings.Count} readings outside the normal range.",
                "Inspect",
                "High"));
        }

        return recommendations;
    }
    /// <summary>
    /// Generates a list of recommendations based on the current state of devices and user interactions. The method checks for active error states in the sensor registry and creates recommendations for devices that are in a "Critical" or "Disconnected" state. It also analyzes user interactions to identify devices that have been frequently searched while having an active problem, generating additional recommendations for those devices. The final list of recommendations is grouped to remove duplicates and returned as a read-only list.
    /// </summary>
    /// <returns></returns>
    public IReadOnlyList<Recommendation> GetRecommendations()
    {
        var recommendations = new List<Recommendation>();

        var activeErrors = _sensorRegistry.GetActiveErrorStates();

        foreach (var errorState in activeErrors)
        {
            var separatorIndex = errorState.IndexOf(':');

            if (separatorIndex <= 0) continue;

            var deviceId = errorState[..separatorIndex];
            var state = errorState[(separatorIndex + 1)..];

            if (state.Equals("Critical", StringComparison.OrdinalIgnoreCase))
            {
                recommendations.Add(new Recommendation(
                    deviceId,
                    "Inspect critical device",
                    $"Device {deviceId} currently has a Critical telemetry state.",
                    "Inspect",
                    "High"));
            }
            else if (state.Equals("Disconnected", StringComparison.OrdinalIgnoreCase))
            {
                recommendations.Add(new Recommendation(
                    deviceId,
                    "Reconnect device",
                    $"No recent telemetry has been received from {deviceId}.",
                    "Start",
                    "High"));
            }
        }

        foreach (var interaction in GetInteractions()
                     .Where(x => x.Action.Equals("Search", StringComparison.OrdinalIgnoreCase))
                     .Where(x => !string.IsNullOrWhiteSpace(x.DeviceId))
                     .GroupBy(x => x.DeviceId!, StringComparer.OrdinalIgnoreCase))
        {
            var deviceId = interaction.Key;
            var searchCount = interaction.Count();

            if (searchCount < 2) continue;

            var hasActiveProblem = activeErrors.Any(error => error.StartsWith($"{deviceId}:", StringComparison.OrdinalIgnoreCase));

            if (!hasActiveProblem) continue;

            recommendations.Add(new Recommendation(
                deviceId,
                "Frequently searched problem device",
                $"You searched for {deviceId} {searchCount} times while it has an active problem.",
                "Inspect",
                "High"));
        }

        recommendations.AddRange(GetTelemetryPatternRecommendations());

        return recommendations
            .GroupBy(
                recommendation => $"{recommendation.DeviceId}:{recommendation.Title}",
                StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
    }
}