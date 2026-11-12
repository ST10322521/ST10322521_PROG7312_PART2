using SmartX.Api.Models;
using SmartX.Shared.Models;

namespace SmartX.Api.Services;

// https://stackoverflow.com/questions/22645024/when-would-i-use-task-yield
public sealed class RecommendationService
{
    private readonly LiveSensorRegistry _sensorRegistry;
    private readonly TelemetryHistoryService _historyService;

    private readonly Queue<UserInteraction> _interactionHistory = new();
    private readonly Dictionary<string, int> _searchFrequency = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _processedAlertIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    public RecommendationService(
        LiveSensorRegistry sensorRegistry,
        TelemetryHistoryService historyService)
    {
        _sensorRegistry = sensorRegistry;
        _historyService = historyService;
    }
    /// <summary>
    /// Records a user interaction, such as a search or inspection action, and maintains a history of the most recent 500 interactions. The method is thread-safe and locks the internal queue to ensure that no other thread can modify it while the interaction is being recorded. If the queue exceeds 500 entries, the oldest entry is removed to maintain the size limit.
    /// </summary>
    /// <param name="interaction"></param>
    public void RecordInteraction(UserInteraction interaction)
    {
        lock (_lock)
        {
            _interactionHistory.Enqueue(interaction);

            // Update search frequency for "Search" actions
            if (interaction.Action.Equals("Search", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(interaction.DeviceId))
            {
                _searchFrequency[interaction.DeviceId] =
                    _searchFrequency.GetValueOrDefault(interaction.DeviceId) + 1;
            }

            // Ensure the list does not exceed 500 entries
            if (_interactionHistory.Count > 500)
            {
                _interactionHistory.Dequeue();
            }
        }
    }
    /// <summary>
    /// Retrieves a read-only list of user interactions, ordered by timestamp in descending order. The method is thread-safe and locks the internal queue to ensure that no other thread can modify it while the list is being generated. It returns a copy of the interaction history as a list, allowing external code to access the data without modifying the original queue.
    /// </summary>
    /// <returns></returns>
    public IReadOnlyList<UserInteraction> GetInteractions()
    {
        lock (_lock)
        {
            return _interactionHistory.OrderByDescending(x => x.Timestamp).ToList();
        }
    }
    /// <summary>
    /// Retrieves the count of "Search" actions performed by the user for a specific device. The method is thread-safe and locks the internal queue to ensure that no other thread can modify it while the count is being calculated. It filters the interactions to include only those that match the specified device ID and have an action type of "Search", then returns the total count of such interactions.
    /// </summary>
    /// <param name="deviceId"></param>
    /// <returns></returns>
    public int GetSearchCount(string deviceId)
    {
        lock (_lock)
        {
            return _interactionHistory.Count(x =>
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
    /// Asynchronously generates a list of recommendations based on the current state of devices and user interactions. The method checks for active error states in the sensor registry and creates recommendations for devices that are in a "Critical" or "Disconnected" state. It also analyzes user interactions to identify devices that have been frequently searched while having an active problem, generating additional recommendations for those devices. The final list of recommendations is grouped to remove duplicates and returned as a read-only list.
    /// </summary>
    /// <returns></returns>
    public async Task<IReadOnlyList<Recommendation>> GetRecommendationsAsync()
    {
        await Task.Yield();

        var recommendations = new List<Recommendation>();

        var activeErrors = _sensorRegistry.GetActiveErrorStates();

        foreach (var errorState in activeErrors)
        {
            var separatorIndex = errorState.IndexOf(':');

            if (separatorIndex <= 0) continue;

            var deviceId = errorState[..separatorIndex];
            var state = errorState[(separatorIndex + 1)..];

            var alertId = $"{deviceId}:{state}";

            // Ensure that each alert is processed only once to avoid duplicate recommendations
            lock (_lock)
            {
                if (!_processedAlertIds.Add(alertId))
                {
                    continue;
                }
            }

            // Generate recommendations based on the state of the device
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

        // Analyze user interactions for frequently searched devices with active problems
        foreach (var frequency in GetSearchFrequencies())
        {
            var deviceId = frequency.Key;
            var searchCount = frequency.Value;

            if (searchCount < 2) continue;

            var hasActiveProblem = activeErrors.Any(error =>
                error.StartsWith($"{deviceId}:", StringComparison.OrdinalIgnoreCase));

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

    /// <summary>
    /// Retrieves a read-only dictionary of search frequencies for devices, indicating how many times each device has been searched by the user. The method is thread-safe and locks the internal dictionary to ensure that no other thread can modify it while the dictionary is being generated. It returns a copy of the search frequency dictionary, allowing external code to access the data without modifying the original dictionary.
    /// </summary>
    /// <returns></returns>
    private IReadOnlyDictionary<string, int> GetSearchFrequencies()
    {
        lock (_lock)
        {
            return new Dictionary<string, int>(_searchFrequency, StringComparer.OrdinalIgnoreCase);
        }
    }
}