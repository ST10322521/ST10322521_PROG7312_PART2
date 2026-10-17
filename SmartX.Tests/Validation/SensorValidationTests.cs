using SmartX.Shared.Enums;
using SmartX.Shared.DTOs;
using SmartX.Shared.Validation;

namespace SmartX.Tests.Validation;

public class SensorValidationTests
{
    [Fact]
    public void Validate_ValidTemperatureSensor_ReturnsNoErrors()
    {
        var request = new CreateSensorRequest
        {
            DeviceId = "TEST-001",
            MacAddress = "AA:BB:CC:DD:EE:01",
            Location = "Test Facility",
            Category = SensorCategory.Environmental,
            TelemetryType = TelemetryType.Temperature,
            SamplingIntervalSeconds = 5,
            NormalMin = 18,
            NormalMax = 28
        };

        var errors = SensorValidation.Validate(request);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_InvalidMacAddress_ReturnsError()
    {
        var request = new CreateSensorRequest
        {
            DeviceId = "TEST-001",
            MacAddress = "INVALID",
            Location = "Test Facility",
            Category = SensorCategory.Environmental,
            TelemetryType = TelemetryType.Temperature,
            SamplingIntervalSeconds = 5,
            NormalMin = 18,
            NormalMax = 28
        };

        var errors = SensorValidation.Validate(request);

        Assert.Contains("Invalid MacAddress format.", errors);
    }

    [Fact]
    public void Validate_InvalidCategoryTelemetryCombination_ReturnsError()
    {
        var request = new CreateSensorRequest
        {
            DeviceId = "TEST-001",
            MacAddress = "AA:BB:CC:DD:EE:01",
            Location = "Test Facility",
            Category = SensorCategory.Environmental,
            TelemetryType = TelemetryType.Power,
            SamplingIntervalSeconds = 5,
            NormalMin = 500,
            NormalMax = 2000
        };

        var errors = SensorValidation.Validate(request);

        Assert.Contains(
            "Power is not valid for Environmental category.",
            errors);
    }

    [Fact]
    public void Validate_InvalidNumericRange_ReturnsError()
    {
        var request = new CreateSensorRequest
        {
            DeviceId = "TEST-001",
            MacAddress = "AA:BB:CC:DD:EE:01",
            Location = "Test Facility",
            Category = SensorCategory.Environmental,
            TelemetryType = TelemetryType.Temperature,
            SamplingIntervalSeconds = 5,
            NormalMin = 30,
            NormalMax = 20
        };

        var errors = SensorValidation.Validate(request);

        Assert.Contains(
            "NormalMin must be less than NormalMax for numeric telemetry types.",
            errors);
    }

    [Fact]
    public void Validate_BooleanTelemetry_IgnoresNormalRange()
    {
        var request = new CreateSensorRequest
        {
            DeviceId = "TEST-001",
            MacAddress = "AA:BB:CC:DD:EE:01",
            Location = "Test Facility",
            Category = SensorCategory.Actuator,
            TelemetryType = TelemetryType.RelayState,
            SamplingIntervalSeconds = 5,
            NormalMin = 100,
            NormalMax = 10
        };

        var errors = SensorValidation.Validate(request);

        Assert.Empty(errors);
    }
}