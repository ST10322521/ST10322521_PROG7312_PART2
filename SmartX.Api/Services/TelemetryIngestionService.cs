
using System.Globalization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SmartX.Api.Data;
using SmartX.Api.Hubs;
using SmartX.Api.Services.Interfaces;
using SmartX.Shared.DTOs;
using SmartX.Shared.Enums;
using SmartX.Shared.Models;

namespace SmartX.Api.Services;

public class TelemetryIngestionService : ITelemetryIngestionService
{
    private readonly SmartXDbContext _context;
    private readonly TelemetryService _telemetryService;
    private readonly LiveSensorRegistry? _sensorRegistry;
    private readonly TelemetryReadingState _readingState;
    private readonly TelemetryHistoryService _historyService;

    public TelemetryIngestionService(
    SmartXDbContext context,
    TelemetryService telemetryService,
    LiveSensorRegistry? sensorRegistry,
    TelemetryHistoryService historyService
    , TelemetryReadingState readingState)
    {
        _context = context;
        _telemetryService = telemetryService;
        _sensorRegistry = sensorRegistry;
        _historyService = historyService;
        _readingState = readingState;
    }
    /// <summary>
    /// Processes incoming telemetry data from sensors. The method takes a TelemetryRequest object as input, which contains the device ID, timestamp, telemetry type, and telemetry value. It first checks if the sensor exists in the LiveSensorRegistry. If the sensor is not found or the telemetry type does not match the registered sensor's telemetry type, it returns null or throws an exception. Depending on the telemetry type, it processes numeric or boolean values accordingly, updating the previous readings and adding the current reading to the TelemetryHistoryService. Finally, it returns a TelemetryResponse object containing the processed data and status.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public async Task<TelemetryResponse?> ReceiveTelemetryAsync(TelemetryRequest request)
    {
        // Check if the sensor exists in the registry
        if (_sensorRegistry is null || !_sensorRegistry.TryGetByDeviceId(request.DeviceId, out var sensor) || sensor is null)
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

                    var currentReading = new SensorReading(request.DeviceId, timestamp, request.TelemetryType, numericValue);

                    _historyService.AddReading(currentReading);

                    double? delta = null;

                    if (_readingState.TryGetPrevious(request.DeviceId, out var previousReading))
                    {
                        var deltaReading = currentReading - previousReading;
                        delta = deltaReading.Value;
                    }

                    _readingState.SetPrevious(currentReading);

                    var response = new TelemetryResponse(
                        request.DeviceId,
                        timestamp,
                        request.TelemetryType,
                        numericValue,
                        status,
                        delta);

                    if (status == TelemetryStatus.Critical)
                    {
                        _sensorRegistry.AddErrorState(request.DeviceId, status.ToString());
                    }
                    else
                    {
                        _sensorRegistry?.RemoveErrorState(request.DeviceId, status.ToString());
                    }

                    return response;
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

                    var response = new TelemetryResponse(
                        request.DeviceId,
                        timestamp,
                        request.TelemetryType,
                        booleanValue,
                        status);

                    return response;
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