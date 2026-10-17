using SmartX.Api.Services;
using SmartX.Shared.Enums;
using SmartX.Shared.Models;

namespace SmartX.Tests.Services;

public class TelemetryServiceTests
{
    private readonly TelemetryService _service = new();

    [Fact]
    public void Process_FloatValueInsideNormalRange_ReturnsNormal()
    {
        var packet = new TelemetryPacket<float>
        {
            DeviceId = "TEST-001",
            Timestamp = DateTime.UtcNow,
            TelemetryType = TelemetryType.Temperature,
            Value = 23.5f
        };

        var result = _service.Process(packet, 18, 28);

        Assert.Equal(TelemetryStatus.Normal, result);
    }

    [Fact]
    public void Process_FloatValueInsideWarningMargin_ReturnsWarning()
    {
        var packet = new TelemetryPacket<float>
        {
            DeviceId = "TEST-001",
            Timestamp = DateTime.UtcNow,
            TelemetryType = TelemetryType.Temperature,
            Value = 17.5f
        };

        var result = _service.Process(packet, 18, 28);

        Assert.Equal(TelemetryStatus.Warning, result);
    }

    [Fact]
    public void Process_FloatValueOutsideWarningMargin_ReturnsCritical()
    {
        var packet = new TelemetryPacket<float>
        {
            DeviceId = "TEST-001",
            Timestamp = DateTime.UtcNow,
            TelemetryType = TelemetryType.Temperature,
            Value = 16f
        };

        var result = _service.Process(packet, 18, 28);

        Assert.Equal(TelemetryStatus.Critical, result);
    }

    [Fact]
    public void Process_IntValueInsideNormalRange_ReturnsNormal()
    {
        var packet = new TelemetryPacket<int>
        {
            DeviceId = "TEST-POWER-001",
            Timestamp = DateTime.UtcNow,
            TelemetryType = TelemetryType.Power,
            Value = 1450
        };

        var result = _service.Process(packet, 500, 2000);

        Assert.Equal(TelemetryStatus.Normal, result);
    }

    [Fact]
    public void Process_BooleanTrue_ReturnsNormal()
    {
        var packet = new TelemetryPacket<bool>
        {
            DeviceId = "TEST-RELAY-001",
            Timestamp = DateTime.UtcNow,
            TelemetryType = TelemetryType.RelayState,
            Value = true
        };

        var result = _service.Process(packet);

        Assert.Equal(TelemetryStatus.Normal, result);
    }

    [Fact]
    public void Process_BooleanFalse_ReturnsWarning()
    {
        var packet = new TelemetryPacket<bool>
        {
            DeviceId = "TEST-RELAY-001",
            Timestamp = DateTime.UtcNow,
            TelemetryType = TelemetryType.RelayState,
            Value = false
        };

        var result = _service.Process(packet);

        Assert.Equal(TelemetryStatus.Warning, result);
    }
}