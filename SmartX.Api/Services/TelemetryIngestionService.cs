
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
    private readonly IHubContext<SmartXHub> _hubContext;
    private readonly Dictionary<string, SensorReading> _previousNumericReadings = new();

    public TelemetryIngestionService(SmartXDbContext context, TelemetryService telemetryService, IHubContext<SmartXHub> hubContext)
    {
        _context = context;
        _telemetryService = telemetryService;
        _hubContext = hubContext;
    }
    /// <summary>
    /// Receives telemetry data asynchronously. The method takes a TelemetryRequest object as input, which contains the device ID, timestamp, telemetry type, and value. It first checks if the sensor associated with the device ID exists in the database. If the sensor is found, it validates that the telemetry type matches the registered sensor's telemetry type. Depending on the telemetry type, it processes numeric or boolean values accordingly, calculates any delta from previous readings, and constructs a TelemetryResponse object. The response is then sent to all connected clients via SignalR. If any validation fails or if the sensor does not exist, appropriate exceptions are thrown or null is returned.  
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

                    var currentReading = new SensorReading(request.DeviceId, timestamp, request.TelemetryType, numericValue);

                    double? delta = null;

                    if (_previousNumericReadings.TryGetValue(request.DeviceId, out var previousReading))
                    {
                        var deltaReading = currentReading - previousReading;
                        delta = deltaReading.Value;
                    }

                    _previousNumericReadings[request.DeviceId] = currentReading;

                    var response = new TelemetryResponse(
                        request.DeviceId,
                        timestamp,
                        request.TelemetryType,
                        numericValue,
                        status,
                        delta);

                    await _hubContext.Clients.All.SendAsync("TelemetryReceived", response);

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

                    await _hubContext.Clients.All.SendAsync("TelemetryReceived", response);

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