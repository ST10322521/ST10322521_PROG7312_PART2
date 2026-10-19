using SmartX.Shared.Models;

// https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/lock

namespace SmartX.Api.Services;

public class TelemetryHistoryService
{
    private readonly Dictionary<string, SortedDictionary<DateTime, SensorReading>> _history = new();
    private readonly object _lock = new();
    /// <summary>
    /// Adds a new telemetry reading to the history for a specific device. The method takes a SensorReading object as input, which contains the device ID, timestamp, telemetry type, and value. It first checks if there is an existing entry for the device ID in the history dictionary. If not, it creates a new SortedDictionary to store the readings for that device. The method then adds the new reading to the SortedDictionary, using the timestamp as the key. The use of a lock ensures thread safety when accessing and modifying the history dictionary.
    /// </summary>
    /// <param name="reading"></param>
    public void AddReading(SensorReading reading)
    {
        lock (_lock)
        {
            if (!_history.TryGetValue(reading.DeviceId, out var readings))
            {
                readings = new SortedDictionary<DateTime, SensorReading>();
                _history[reading.DeviceId] = readings;
            }

            readings[reading.Timestamp] = reading;
        }
    }
    /// <summary>
    /// Retrieves the telemetry history for a specific device. The method takes the device ID as an input parameter and returns a list of SensorReading objects associated with that device. If there is no entry for the specified device ID in the history dictionary, it returns an empty list. The method uses a lock to ensure thread safety when accessing the history dictionary.
    /// </summary>
    /// <param name="deviceId"></param>
    /// <returns></returns>
    public IReadOnlyList<SensorReading> GetHistory(string deviceId)
    {
        lock (_lock)
        {
            if (!_history.TryGetValue(deviceId, out var readings))
            {
                return Array.Empty<SensorReading>();
            }

            return readings.Values.ToList();
        }
    }
    /// <summary>
    /// Retrieves the telemetry history for a specific device within a given time range. The method takes the device ID, start time (from), and end time (to) as input parameters. It first checks if there is an existing entry for the device ID in the history dictionary. If not, it returns an empty list. If an entry exists, it filters the readings to include only those within the specified time range and returns them as a list of SensorReading objects. The method uses a lock to ensure thread safety when accessing the history dictionary.
    /// </summary>
    /// <param name="deviceId"></param>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <returns></returns>
    public IReadOnlyList<SensorReading> GetHistory(string deviceId, DateTime from, DateTime to)
    {
        lock (_lock)
        {
            if (!_history.TryGetValue(deviceId, out var readings))
            {
                return Array.Empty<SensorReading>();
            }

            return readings
                .Where(pair => pair.Key >= from && pair.Key <= to)
                .Select(pair => pair.Value)
                .ToList();
        }
    }
    /// <summary>
    /// Retrieves the count of telemetry readings for a specific device. The method takes the device ID as an input parameter and returns the number of readings associated with that device. If there is no entry for the specified device ID in the history dictionary, it returns zero. The method uses a lock to ensure thread safety when accessing the history dictionary.
    /// </summary>
    /// <param name="deviceId"></param>
    /// <returns></returns>
    public int GetReadingCount(string deviceId)
    {
        lock (_lock)
        {
            return _history.TryGetValue(deviceId, out var readings) ? readings.Count : 0;
        }
    }
    /// <summary>
    /// Clears the telemetry history for a specific device. The method takes the device ID as an input parameter and removes the corresponding entry from the history dictionary. If there is no entry for the specified device ID, the method does nothing. The method uses a lock to ensure thread safety when accessing and modifying the history dictionary.
    /// </summary>
    /// <param name="deviceId"></param>
    public void Clear(string deviceId)
    {
        lock (_lock)
        {
            _history.Remove(deviceId);
        }
    }
}