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