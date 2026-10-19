using SmartX.Shared.Models;

namespace SmartX.Api.Services;

public sealed class TelemetryReadingState
{
    private readonly Dictionary<string, SensorReading> _previousReadings = new();

    public bool TryGetPrevious(string deviceId, out SensorReading reading)
    {
        lock (_previousReadings)
        {
            return _previousReadings.TryGetValue(deviceId, out reading);
        }
    }

    public void SetPrevious(SensorReading reading)
    {
        lock (_previousReadings)
        {
            _previousReadings[reading.DeviceId] = reading;
        }
    }
}