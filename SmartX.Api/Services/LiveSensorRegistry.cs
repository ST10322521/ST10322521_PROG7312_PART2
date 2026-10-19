using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartX.Api.Data;
using SmartX.Shared.Models;

namespace SmartX.Api.Services;

/// <summary>
/// Represents a registry for managing live sensor information. This class provides methods to retrieve sensor data based on device IDs or MAC addresses, as well as a method to refresh the registry by querying the database for the latest sensor information. The registry maintains two internal dictionaries for efficient lookups and ensures thread safety through locking mechanisms. It is designed to be used as a singleton service within the application.
/// </summary>
public sealed class LiveSensorRegistry
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Dictionary<string, Sensor> _devicesByDeviceId = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Sensor> _devicesByMac = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _activeErrorStates = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DateTime> _lastSeen = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    public LiveSensorRegistry(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _devicesByDeviceId.Count;
            }
        }
    }
    /// <summary>
    /// Attempts to retrieve a Sensor object from the registry based on the provided deviceId. The method uses a lock to ensure thread safety while accessing the internal dictionary that maps device IDs to Sensor objects. If a Sensor with the specified deviceId exists in the registry, it is returned through the out parameter 'sensor', and the method returns true. If no such Sensor exists, the method returns false, and 'sensor' is set to null.
    /// </summary>
    /// <param name="deviceId"></param>
    /// <param name="sensor"></param>
    /// <returns></returns>
    public bool TryGetByDeviceId(string deviceId, out Sensor? sensor)
    {
        lock (_lock)
        {
            return _devicesByDeviceId.TryGetValue(deviceId, out sensor);
        }
    }
    /// <summary>
    /// Attempts to retrieve a Sensor object from the registry based on the provided MAC address. The method uses a lock to ensure thread safety while accessing the internal dictionary that maps MAC addresses to Sensor objects. If a Sensor with the specified MAC address exists in the registry, it is returned through the out parameter 'sensor', and the method returns true. If no such Sensor exists, the method returns false, and 'sensor' is set to null.
    /// </summary>
    /// <param name="mac"></param>
    /// <param name="sensor"></param>
    /// <returns></returns>
    public bool TryGetByMac(string mac, out Sensor? sensor)
    {
        lock (_lock)
        {
            return _devicesByMac.TryGetValue(mac, out sensor);
        }
    }
    /// <summary>
    /// Adds an error state for a specific device to the registry. The method uses a lock to ensure thread safety while modifying the internal HashSet that tracks active error states. The error state is represented as a string in the format "deviceId:errorState". If the error state is successfully added to the HashSet, the method returns true; otherwise, it returns false if the error state already exists.
    /// </summary>
    /// <param name="deviceId"></param>
    /// <param name="errorState"></param>
    /// <returns></returns>
    public bool AddErrorState(string deviceId, string errorState)
    {
        lock (_lock)
        {
            return _activeErrorStates.Add($"{deviceId}:{errorState}");
        }
    }
    /// <summary>
    /// Removes an error state for a specific device from the registry. The method uses a lock to ensure thread safety while modifying the internal HashSet that tracks active error states. The error state is represented as a string in the format "deviceId:errorState". If the error state is successfully removed from the HashSet, the method returns true; otherwise, it returns false if the error state does not exist.
    /// </summary>
    /// <param name="deviceId"></param>
    /// <param name="errorState"></param>
    /// <returns></returns>
    public bool RemoveErrorState(string deviceId, string errorState)
    {
        lock (_lock)
        {
            return _activeErrorStates.Remove($"{deviceId}:{errorState}");
        }
    }
    /// <summary>
    /// Retrieves a read-only collection of all active error states currently stored in the registry. The method uses a lock to ensure thread safety while accessing the internal HashSet that tracks active error states. It returns an array containing all the active error states, allowing external components to view the current error conditions without modifying the underlying data structure.
    /// </summary>
    /// <returns></returns>
    public IReadOnlyCollection<string> GetActiveErrorStates()
    {
        lock (_lock)
        {
            return _activeErrorStates.ToArray();
        }
    }
    /// <summary>
    /// Records the timestamp of the last received telemetry data for a specific device. The method uses a lock to ensure thread safety while modifying the internal dictionary that tracks the last seen timestamps for each device. It updates the dictionary with the provided timestamp for the specified deviceId, allowing the registry to keep track of when each device last sent telemetry data.
    /// </summary>
    /// <param name="deviceId"></param>
    /// <param name="timestamp"></param>
    public void RecordTelemetry(string deviceId, DateTime timestamp)
    {
        lock (_lock)
        {
            _lastSeen[deviceId] = timestamp;
        }
    }
    /// <summary>
    /// Retrieves a read-only collection of device IDs that have not sent telemetry data within the specified timeout period. The method uses a lock to ensure thread safety while accessing the internal dictionary that tracks the last seen timestamps for each device. It calculates the current time and compares it with the last seen timestamps to identify devices that have been inactive for longer than the specified timeout. The method also adds a "Disconnected" error state for each disconnected device to the active error states HashSet. Finally, it returns a list of disconnected device IDs.
    /// </summary>
    /// <param name="timeout"></param>
    /// <returns></returns>
    public IReadOnlyCollection<string> GetDisconnectedDevices(TimeSpan timeout)
    {
        var now = DateTime.UtcNow;

        lock (_lock)
        {
            var disconnected = _lastSeen
                .Where(pair => now - pair.Value > timeout)
                .Select(pair => pair.Key)
                .ToList();

            foreach (var deviceId in disconnected)
            {
                _activeErrorStates.Add($"{deviceId}:Disconnected");
            }

            return disconnected;
        }
    }
    /// <summary>
    /// Refreshes the live sensor registry by querying the database for all sensors and updating the internal dictionaries that map device IDs and MAC addresses to Sensor objects. The method creates a new service scope to access the SmartXDbContext, retrieves all sensors from the database without tracking changes, and then clears and repopulates the internal dictionaries with the latest sensor data. The operation is performed within a lock to ensure thread safety while modifying the dictionaries.
    /// </summary>
    /// <returns></returns>
    public async Task RefreshAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SmartXDbContext>();
        var sensors = await context.Sensors.AsNoTracking().ToListAsync();

        lock (_lock)
        {
            _devicesByDeviceId.Clear();
            _devicesByMac.Clear();

            foreach (var sensor in sensors)
            {
                _devicesByDeviceId[sensor.DeviceId] = sensor;
                _devicesByMac[sensor.MacAddress] = sensor;
            }
        }
    }
}