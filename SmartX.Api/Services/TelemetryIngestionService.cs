
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SmartX.Api.Data;
using SmartX.Api.Services.Interfaces;
using SmartX.Shared.DTOs;
using SmartX.Shared.Enums;
using SmartX.Shared.Models;

namespace SmartX.Api.Services;

public class TelemetryIngestionService : ITelemetryIngestionService
{
    private readonly SmartXDbContext _context;
    private readonly TelemetryService _telemetryService;

    public TelemetryIngestionService(SmartXDbContext context, TelemetryService telemetryService)
    {
        _context = context;
        _telemetryService = telemetryService;
    }
    /// <summary>
    /// Receives telemetry data from a sensor and processes it based on the sensor's telemetry type.
    /// If the sensor does not exist, returns null.
    /// If the telemetry type does not match the registered sensor's telemetry type, throws an InvalidOperationException.
    /// If the telemetry value is not of the expected type, throws an ArgumentException.
    /// Returns a TelemetryResponse containing the processed telemetry data and its status.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public async Task<TelemetryResponse?> ReceiveTelemetryAsync(TelemetryRequest request)
    {
        // Check if the sensor exists
        var sensor = await _context.Sensors.AsNoTracking().FirstOrDefaultAsync(s => s.DeviceId == request.DeviceId);
        if (sensor is null)
        {
            return null;
        }

        if (sensor.TelemetryType != request.TelemetryType)
        {
            throw new InvalidOperationException(
                "The provided telemetry type does not match the registered sensor's telemetry type.");
        }

        var timestamp = request.Timestamp == default ? DateTime.UtcNow : request.Timestamp;

        switch (request.TelemetryType)
        {
            // Handle numeric telemetry types
            case TelemetryType.Temperature:
            case TelemetryType.Humidity:
            case TelemetryType.SoilMoisture:
            case TelemetryType.Power:
            case TelemetryType.Voltage:
            case TelemetryType.Current:
                {
                    if (!TryConvertNumericValue(
                        request.Value, out var numericValue))
                    {
                        throw new ArgumentException(
                            "Telemetry value must be numeric.");
                    }

                    TelemetryStatus status;

                    switch (request.TelemetryType)
                    {
                        case TelemetryType.Temperature:
                        case TelemetryType.Humidity:
                        case TelemetryType.SoilMoisture:
                            status = _telemetryService.Process(
                                new TelemetryPacket<float>
                                {
                                    DeviceId = request.DeviceId,
                                    Timestamp = timestamp,
                                    TelemetryType = request.TelemetryType,
                                    Value = (float)numericValue
                                },
                                sensor.NormalMin,
                                sensor.NormalMax);
                            break;

                        default:
                            if (numericValue % 1 != 0 ||
                                numericValue < int.MinValue ||
                                numericValue > int.MaxValue)
                            {
                                throw new ArgumentException(
                                    "This telemetry type requires an integer value.");
                            }

                            status = _telemetryService.Process(
                                new TelemetryPacket<int>
                                {
                                    DeviceId = request.DeviceId,
                                    Timestamp = timestamp,
                                    TelemetryType = request.TelemetryType,
                                    Value = (int)numericValue
                                },
                                sensor.NormalMin,
                                sensor.NormalMax);
                            break;
                    }

                    return new TelemetryResponse(
                        request.DeviceId,
                        timestamp,
                        request.TelemetryType,
                        numericValue,
                        status);
                }

            case TelemetryType.RelayState:
            case TelemetryType.ValveState:
                {
                    if (!TryConvertBooleanValue(
                        request.Value, out var booleanValue))
                    {
                        throw new ArgumentException(
                            "Telemetry value must be boolean.");
                    }

                    var status = _telemetryService.Process(
                        new TelemetryPacket<bool>
                        {
                            DeviceId = request.DeviceId,
                            Timestamp = timestamp,
                            TelemetryType = request.TelemetryType,
                            Value = booleanValue
                        });

                    return new TelemetryResponse(
                        request.DeviceId,
                        timestamp,
                        request.TelemetryType,
                        booleanValue,
                        status);
                }

            default:
                throw new ArgumentException(
                    "Unsupported telemetry type.");
        }
    }

    private static bool TryConvertNumericValue(
        object? value,
        out double numericValue)
    {
        return double.TryParse(
                   value?.ToString(),
                   NumberStyles.Float,
                   CultureInfo.InvariantCulture,
                   out numericValue)
               && double.IsFinite(numericValue);
    }

    private static bool TryConvertBooleanValue(
        object? value,
        out bool booleanValue)
    {
        return bool.TryParse(
            value?.ToString(),
            out booleanValue);
    }
}