using System.Text.RegularExpressions;
using SmartX.Shared.DTOs;
using SmartX.Shared.Enums;

namespace SmartX.Shared.Validation;

public static class SensorValidation
{
    /// <summary>
    /// Validates the CreateSensorRequest object and returns a list of error messages if any validation fails.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public static List<string> Validate(CreateSensorRequest request)
    {
        var errors = new List<String>();

        if (string.IsNullOrWhiteSpace(request.DeviceId))
        {
            errors.Add("DeviceId is required.");
        }
        else if (!IsValidMacAddress(request.MacAddress))
        {
            errors.Add("Invalid MacAddress format.");
        }

        if (string.IsNullOrWhiteSpace(request.Location))
        {
            errors.Add("Location is required.");
        }

        if (request.SamplingIntervalSeconds <= 0)
        {
            errors.Add("SamplingIntervalSeconds must be greater than 0.");
        }

        if (!IsValidTelemetryForCategory(request.Category, request.TelemetryType))
        {
            errors.Add($"{request.TelemetryType} is not valid for {request.Category} category.");
        }

        if (IsNumericTelemetry(request.TelemetryType) && request.NormalMin >= request.NormalMax)
        {
            errors.Add("NormalMin must be less than NormalMax for numeric telemetry types.");
        }

        return errors;
    }

    /// <summary>
    /// Validates the MAC address format using a regular expression.
    /// </summary>
    /// <param name="macAddress"></param>
    /// <returns></returns>
    private static bool IsValidMacAddress(string macAddress)
    {
        return Regex.IsMatch(macAddress, @"^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$");
    }

    /// <summary>
    /// Checks if the given telemetry type is valid for the specified sensor category.
    /// </summary>
    /// <param name="category"></param>
    /// <param name="telemetryType"></param>
    /// <returns></returns>
    public static bool IsValidTelemetryForCategory(SensorCategory category, TelemetryType telemetryType)
    {
        return category switch
        {
            SensorCategory.Environmental =>
                telemetryType is
                    TelemetryType.Temperature or
                    TelemetryType.Humidity or
                    TelemetryType.SoilMoisture,

            SensorCategory.Power =>
                telemetryType is
                    TelemetryType.Voltage or
                    TelemetryType.Current or
                    TelemetryType.Power,

            SensorCategory.Actuator =>
                telemetryType is
                    TelemetryType.RelayState or
                    TelemetryType.ValveState,
            _ => false
        };
    }

    /// <summary>
    /// Checks if the given telemetry type is numeric (Temperature, Humidity, SoilMoisture, Voltage, Current, Power).
    /// </summary>
    /// <param name="telemetryType"></param>
    /// <returns></returns>
    private static bool IsNumericTelemetry(TelemetryType telemetryType)
    {
        return telemetryType is
            TelemetryType.Temperature or
            TelemetryType.Humidity or
            TelemetryType.SoilMoisture or
            TelemetryType.Voltage or
            TelemetryType.Current or
            TelemetryType.Power;
    }
}