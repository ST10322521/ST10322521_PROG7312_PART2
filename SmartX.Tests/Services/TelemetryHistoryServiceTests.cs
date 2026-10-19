using SmartX.Api.Services;
using SmartX.Shared.Enums;
using SmartX.Shared.Models;

namespace SmartX.Tests.Services;

public class TelemetryHistoryServiceTests
{
    [Fact]
    public void AddReading_ReturnsReadingsInTimestampOrder()
    {
        var service = new TelemetryHistoryService();

        var baseTime = DateTime.UtcNow;

        var reading3 = new SensorReading(
            "HISTORY-001",
            baseTime.AddMinutes(3),
            TelemetryType.Temperature,
            23);

        var reading1 = new SensorReading(
            "HISTORY-001",
            baseTime.AddMinutes(1),
            TelemetryType.Temperature,
            21);

        var reading2 = new SensorReading(
            "HISTORY-001",
            baseTime.AddMinutes(2),
            TelemetryType.Temperature,
            22);

        service.AddReading(reading3);
        service.AddReading(reading1);
        service.AddReading(reading2);

        var history = service.GetHistory("HISTORY-001");

        Assert.Equal(3, history.Count);
        Assert.Equal(reading1.Timestamp, history[0].Timestamp);
        Assert.Equal(reading2.Timestamp, history[1].Timestamp);
        Assert.Equal(reading3.Timestamp, history[2].Timestamp);
    }

    [Fact]
    public void GetHistory_WithDateRange_ReturnsOnlyMatchingReadings()
    {
        var service = new TelemetryHistoryService();

        var baseTime = DateTime.UtcNow;

        service.AddReading(new SensorReading(
            "HISTORY-002",
            baseTime.AddMinutes(1),
            TelemetryType.Temperature,
            20));

        service.AddReading(new SensorReading(
            "HISTORY-002",
            baseTime.AddMinutes(2),
            TelemetryType.Temperature,
            21));

        service.AddReading(new SensorReading(
            "HISTORY-002",
            baseTime.AddMinutes(3),
            TelemetryType.Temperature,
            22));

        var history = service.GetHistory(
            "HISTORY-002",
            baseTime.AddMinutes(2),
            baseTime.AddMinutes(3));

        Assert.Equal(2, history.Count);
        Assert.Equal(21, history[0].Value);
        Assert.Equal(22, history[1].Value);
    }

    [Fact]
    public void GetHistory_ForUnknownDevice_ReturnsEmptyCollection()
    {
        var service = new TelemetryHistoryService();

        var history = service.GetHistory("DOES-NOT-EXIST");

        Assert.Empty(history);
    }

    [Fact]
    public void GetReadingCount_ReturnsCorrectCount()
    {
        var service = new TelemetryHistoryService();

        var timestamp = DateTime.UtcNow;

        service.AddReading(new SensorReading(
            "HISTORY-003",
            timestamp,
            TelemetryType.Temperature,
            20));

        service.AddReading(new SensorReading(
            "HISTORY-003",
            timestamp.AddMinutes(1),
            TelemetryType.Temperature,
            21));

        Assert.Equal(2, service.GetReadingCount("HISTORY-003"));
    }

    [Fact]
    public void Clear_RemovesDeviceHistory()
    {
        var service = new TelemetryHistoryService();

        service.AddReading(new SensorReading(
            "HISTORY-004",
            DateTime.UtcNow,
            TelemetryType.Temperature,
            25));

        Assert.Equal(1, service.GetReadingCount("HISTORY-004"));

        service.Clear("HISTORY-004");

        Assert.Equal(0, service.GetReadingCount("HISTORY-004"));
        Assert.Empty(service.GetHistory("HISTORY-004"));
    }
}