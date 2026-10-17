using SmartX.Shared.Enums;

namespace SmartX.Shared.Models;

public readonly record struct SensorReading(
    string DeviceId,
    DateTime Timestamp,
    TelemetryType TelemetryType,
    double Value
)
{
    public static SensorReading operator +(SensorReading left, SensorReading right)
    {
        if (left.TelemetryType != right.TelemetryType)
        {
            throw new InvalidOperationException("Cannot add SensorReadings of different TelemetryTypes.");
        }

        return new SensorReading($"{left.DeviceId}+{right.DeviceId}",
            left.Timestamp > right.Timestamp ? left.Timestamp : right.Timestamp,
            left.TelemetryType,
            left.Value + right.Value);
    }

    public static SensorReading operator -(SensorReading left, SensorReading right)
    {
        if (left.DeviceId != right.DeviceId)
        {
            throw new InvalidOperationException("Cannot subtract SensorReadings from different devices.");
        }
        if (left.TelemetryType != right.TelemetryType)
        {
            throw new InvalidOperationException("Cannot subtract SensorReadings of different TelemetryTypes.");
        }

        return new SensorReading($"{left.DeviceId}",
            left.Timestamp > right.Timestamp ? left.Timestamp : right.Timestamp,
            left.TelemetryType,
            left.Value - right.Value);
    }
}