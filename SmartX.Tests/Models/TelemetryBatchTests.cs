using SmartX.Shared.Enums;
using SmartX.Shared.Models;

namespace SmartX.Tests.Models;

public class TelemetryBatchTests
{
    [Fact]
    public void RectangularArray_StoresAndRetrievesValues()
    {
        var batch = new TelemetryBatch(2, 3);

        batch.SetRectangularValue(0, 0, 1200);
        batch.SetRectangularValue(0, 1, 1250);
        batch.SetRectangularValue(1, 0, 800);

        Assert.Equal(2, batch.SensorCount);
        Assert.Equal(3, batch.ReadingsPerSensor);

        Assert.Equal(1200, batch.GetRectangularValue(0, 0));
        Assert.Equal(1250, batch.GetRectangularValue(0, 1));
        Assert.Equal(800, batch.GetRectangularValue(1, 0));
    }

    [Fact]
    public void JaggedArray_SupportsVariableReadingCounts()
    {
        var batch = new TelemetryBatch([3, 2, 4]);

        batch.SetJaggedValue(0, 0, 1200);
        batch.SetJaggedValue(0, 1, 1250);
        batch.SetJaggedValue(0, 2, 1300);

        batch.SetJaggedValue(1, 0, 800);
        batch.SetJaggedValue(1, 1, 850);

        batch.SetJaggedValue(2, 0, 900);
        batch.SetJaggedValue(2, 1, 950);
        batch.SetJaggedValue(2, 2, 1000);
        batch.SetJaggedValue(2, 3, 1050);

        Assert.Equal(3, batch.SensorCount);

        Assert.Equal(3, batch.GetJaggedReadingCount(0));
        Assert.Equal(2, batch.GetJaggedReadingCount(1));
        Assert.Equal(4, batch.GetJaggedReadingCount(2));

        Assert.Equal(1200, batch.GetJaggedValue(0, 0));
        Assert.Equal(850, batch.GetJaggedValue(1, 1));
        Assert.Equal(1050, batch.GetJaggedValue(2, 3));
    }

    [Fact]
    public void RectangularArray_ConvertsToReadingsList()
    {
        var batch = new TelemetryBatch(2, 2);

        batch.SetRectangularValue(0, 0, 1200);
        batch.SetRectangularValue(0, 1, 1250);
        batch.SetRectangularValue(1, 0, 800);
        batch.SetRectangularValue(1, 1, 850);

        var timestamp = DateTime.UtcNow;

        var readings = batch.ToReadings(
            ["POWER-A", "POWER-B"],
            TelemetryType.Power,
            timestamp);

        Assert.Equal(4, readings.Count);

        Assert.Equal("POWER-A", readings[0].DeviceId);
        Assert.Equal(1200, readings[0].Value);

        Assert.Equal("POWER-A", readings[1].DeviceId);
        Assert.Equal(1250, readings[1].Value);

        Assert.Equal("POWER-B", readings[2].DeviceId);
        Assert.Equal(800, readings[2].Value);

        Assert.Equal("POWER-B", readings[3].DeviceId);
        Assert.Equal(850, readings[3].Value);
    }

    [Fact]
    public void JaggedArray_ConvertsVariableReadingsToList()
    {
        var batch = new TelemetryBatch([2, 1, 3]);

        batch.SetJaggedValue(0, 0, 1200);
        batch.SetJaggedValue(0, 1, 1250);

        batch.SetJaggedValue(1, 0, 800);

        batch.SetJaggedValue(2, 0, 900);
        batch.SetJaggedValue(2, 1, 950);
        batch.SetJaggedValue(2, 2, 1000);

        var timestamp = DateTime.UtcNow;

        var readings = batch.ToReadings(
            ["POWER-A", "POWER-B", "POWER-C"],
            TelemetryType.Power,
            timestamp);

        Assert.Equal(6, readings.Count);

        Assert.Equal("POWER-A", readings[0].DeviceId);
        Assert.Equal(1200, readings[0].Value);

        Assert.Equal("POWER-A", readings[1].DeviceId);
        Assert.Equal(1250, readings[1].Value);

        Assert.Equal("POWER-B", readings[2].DeviceId);
        Assert.Equal(800, readings[2].Value);

        Assert.Equal("POWER-C", readings[3].DeviceId);
        Assert.Equal(900, readings[3].Value);

        Assert.Equal("POWER-C", readings[5].DeviceId);
        Assert.Equal(1000, readings[5].Value);
    }

    [Fact]
    public void ToReadings_RejectsIncorrectDeviceCount()
    {
        var batch = new TelemetryBatch(2, 2);

        Assert.Throws<ArgumentException>(() =>
            batch.ToReadings(
                ["POWER-A"],
                TelemetryType.Power,
                DateTime.UtcNow));
    }

    [Fact]
    public void Constructor_RejectsInvalidRectangularDimensions()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TelemetryBatch(0, 2));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TelemetryBatch(2, 0));
    }

    [Fact]
    public void Constructor_RejectsInvalidJaggedDimensions()
    {
        Assert.Throws<ArgumentException>(() =>
            new TelemetryBatch([]));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TelemetryBatch([2, 0, 3]));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TelemetryBatch([2, -1, 3]));
    }

    [Fact]
    public void RectangularBatch_RejectsJaggedOperations()
    {
        var batch = new TelemetryBatch(2, 2);

        Assert.Throws<InvalidOperationException>(() =>
            batch.SetJaggedValue(0, 0, 100));

        Assert.Throws<InvalidOperationException>(() =>
            batch.GetJaggedValue(0, 0));
    }

    [Fact]
    public void JaggedBatch_RejectsRectangularOperations()
    {
        var batch = new TelemetryBatch([2, 3]);

        Assert.Throws<InvalidOperationException>(() =>
            batch.SetRectangularValue(0, 0, 100));

        Assert.Throws<InvalidOperationException>(() =>
            batch.GetRectangularValue(0, 0));

        Assert.Throws<InvalidOperationException>(() =>
            _ = batch.ReadingsPerSensor);
    }
}