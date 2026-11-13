using SmartX.Shared.DataStructures.Trees;
using SmartX.Shared.Enums;
using SmartX.Shared.Models;

// https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/lock

namespace SmartX.Api.Services;

public class TelemetryHistoryService
{
    private readonly Dictionary<string, AvlTree<DateTime, SensorReading>> _history = new();
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
                readings = new AvlTree<DateTime, SensorReading>();
                _history[reading.DeviceId] = readings;
            }

            readings.Insert(reading.Timestamp, reading);
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

            return readings
                .InOrder()
                .Select(pair => pair.Value)
                .ToList();
        }
    }
    /// <summary>
    /// Retrieves the telemetry history for a specific device within a specified time range. The method takes the device ID, telemetry type, and two DateTime parameters (from and to) as input. It returns a list of SensorReading objects that match the specified telemetry type and fall within the given time range. If the 'from' date is later than the 'to' date, it returns an empty list. The method uses a lock to ensure thread safety when accessing the history dictionary.   
    /// </summary>
    /// <param name="deviceId"></param>
    /// <param name="telemetryType"></param>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <returns></returns>
    public IReadOnlyList<SensorReading> GetHistory(
        string deviceId,
        TelemetryType telemetryType,
        DateTime from,
        DateTime to)
    {
        if (from > to)
        {
            return Array.Empty<SensorReading>();
        }

        // Use a lock to ensure thread safety when accessing the history dictionary
        lock (_lock)
        {
            if (!_history.TryGetValue(deviceId, out var readings))
            {
                return Array.Empty<SensorReading>();
            }

            return readings
                .Range(from, to)
                .Select(pair => pair.Value)
                .Where(reading => reading.TelemetryType == telemetryType)
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
    /// <summary>
    /// Attempts to retrieve the latest telemetry reading for a specific device. The method takes the device ID as an input parameter and outputs the latest SensorReading object if available. It first checks if there is an existing entry for the device ID in the history dictionary and if there are any readings present. If a latest reading is found, it is returned through the out parameter, and the method returns true. If no readings are found or the device ID does not exist, the method returns false, and the out parameter is set to its default value. The method uses a lock to ensure thread safety when accessing the history dictionary.
    /// </summary>
    /// <param name="deviceId"></param>
    /// <param name="reading"></param>
    /// <returns></returns>
    public bool TryGetLatestReading(string deviceId, out SensorReading reading)
    {
        lock (_lock)
        {
            if (_history.TryGetValue(deviceId, out var readings) &&
                readings.Count > 0)
            {
                if (readings.TryGetMaximum(out var latest))
                {
                    reading = latest.Value;
                    return true;
                }
            }

            reading = default;
            return false;
        }
    }
}