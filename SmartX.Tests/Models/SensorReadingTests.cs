using SmartX.Shared.Enums;
using SmartX.Shared.Models;

namespace SmartX.Tests.Models;

public class SensorReadingTests
{
    [Fact]
    public void Add_SameTelemetryType_AggregatesValues()
    {
        var floorA = new SensorReading(
            "POWER-A",
            DateTime.UtcNow,
            TelemetryType.Power,
            1200);

        var floorB = new SensorReading(
            "POWER-B",
            DateTime.UtcNow,
            TelemetryType.Power,
            800);

        var result = floorA + floorB;

        Assert.Equal(TelemetryType.Power, result.TelemetryType);
        Assert.Equal(2000, result.Value);
    }

    [Fact]
    public void Add_DifferentTelemetryTypes_ThrowsInvalidOperationException()
    {
        var power = new SensorReading(
            "POWER-A",
            DateTime.UtcNow,
            TelemetryType.Power,
            1200);

        var temperature = new SensorReading(
            "TEMP-A",
            DateTime.UtcNow,
            TelemetryType.Temperature,
            25);

        Assert.Throws<InvalidOperationException>(
            () => power + temperature);
    }

    [Fact]
    public void Subtract_SameSensor_ReturnsTelemetryDelta()
    {
        var previous = new SensorReading(
            "POWER-A",
            DateTime.UtcNow.AddMinutes(-5),
            TelemetryType.Power,
            1200);

        var current = new SensorReading(
            "POWER-A",
            DateTime.UtcNow,
            TelemetryType.Power,
            1650);

        var result = current - previous;

        Assert.Equal("POWER-A", result.DeviceId);
        Assert.Equal(TelemetryType.Power, result.TelemetryType);
        Assert.Equal(450, result.Value);
    }

    [Fact]
    public void Subtract_DifferentSensors_ThrowsInvalidOperationException()
    {
        var first = new SensorReading(
            "POWER-A",
            DateTime.UtcNow,
            TelemetryType.Power,
            1650);

        var second = new SensorReading(
            "POWER-B",
            DateTime.UtcNow,
            TelemetryType.Power,
            1200);

        Assert.Throws<InvalidOperationException>(
            () => first - second);
    }

    [Fact]
    public void Subtract_DifferentTelemetryTypes_ThrowsInvalidOperationException()
    {
        var power = new SensorReading(
            "POWER-A",
            DateTime.UtcNow,
            TelemetryType.Power,
            1650);

        var temperature = new SensorReading(
            "POWER-A",
            DateTime.UtcNow,
            TelemetryType.Temperature,
            25);

        Assert.Throws<InvalidOperationException>(
            () => power - temperature);
    }
}