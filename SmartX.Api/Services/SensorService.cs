
using Microsoft.EntityFrameworkCore;
using SmartX.Api.Data;
using SmartX.Api.Results;
using SmartX.Api.Services.Interfaces;
using SmartX.Shared.DTOs;
using SmartX.Shared.Models;
using SmartX.Shared.Validation;

namespace SmartX.Api.Services;

// https://learn.microsoft.com/en-us/dotnet/api/system.data.entity.dbextensions.asnotracking?view=entity-framework-5.0.0
public class SensorService : ISensorService
{
    private readonly SmartXDbContext _context;

    public SensorService(SmartXDbContext context)
    {
        _context = context;
    }
    /// <summary>
    /// Retrieves all sensors from the database.
    /// </summary>
    /// <returns></returns>
    public async Task<IReadOnlyList<Sensor>> GetAllAsync()
    {
        return await _context.Sensors.AsNoTracking().ToListAsync();
    }
    /// <summary>
    /// Retrieves a specific sensor by its unique identifier (ID).
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<Sensor?> GetByIdAsync(Guid id)
    {
        return await _context.Sensors.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
    }
    /// <summary>
    /// Creates a new sensor based on the provided request data.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<ServiceResult<Sensor>> CreateAsync(CreateSensorRequest request)
    {
        // Validate the request
        var errors = SensorValidation.Validate(request);
        if (errors.Any())
        {
            return new(
                ServiceStatus.ValidationFailure,
                Errors: errors);
        }

        // Check for existing DeviceId or MacAddress
        var deviceIdExists = await _context.Sensors.AnyAsync(s => s.DeviceId == request.DeviceId);
        var macAddressExists = await _context.Sensors.AnyAsync(s => s.MacAddress == request.MacAddress);
        if (deviceIdExists || macAddressExists)
        {
            return new(
                ServiceStatus.Conflict,
                Error: "A sensor with the same DeviceId or MacAddress already exists.");
        }

        var sensor = new Sensor
        {
            Id = Guid.NewGuid(),
            DeviceId = request.DeviceId,
            MacAddress = request.MacAddress,
            Location = request.Location,
            Category = request.Category,
            TelemetryType = request.TelemetryType,
            SamplingIntervalSeconds = request.SamplingIntervalSeconds,
            NormalMin = request.NormalMin,
            NormalMax = request.NormalMax,
            IsActive = true
        };

        _context.Sensors.Add(sensor);
        await _context.SaveChangesAsync();

        return new(ServiceStatus.Success, Data: sensor);
    }
    /// <summary>
    /// Updates an existing sensor based on the provided ID and request data.
    /// If the sensor does not exist, a NotFound response is returned.
    /// If the request data is invalid, a BadRequest response is returned.
    /// If there is a conflict with existing sensors (e.g., duplicate DeviceId or MacAddress), a Conflict response is returned.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<ServiceResult<Sensor>> UpdateAsync(Guid id, CreateSensorRequest request)
    {
        // Validate the request
        var errors = SensorValidation.Validate(request);
        if (errors.Any())
        {
            return new(
                ServiceStatus.ValidationFailure,
                Errors: errors);
        }

        // Check if the sensor exists
        var sensor = await _context.Sensors.FirstOrDefaultAsync(s => s.Id == id);
        if (sensor is null)
        {
            return new(ServiceStatus.NotFound);
        }

        // Check for existing DeviceId or MacAddress (excluding the current sensor)
        var deviceIdExists = await _context.Sensors.AnyAsync(s => s.DeviceId == request.DeviceId && s.Id != id);
        var macAddressExists = await _context.Sensors.AnyAsync(s => s.MacAddress == request.MacAddress && s.Id != id);
        if (deviceIdExists || macAddressExists)
        {
            return new(
                ServiceStatus.Conflict,
                Error: "A sensor with the same DeviceId or MacAddress already exists.");
        }

        sensor.DeviceId = request.DeviceId;
        sensor.MacAddress = request.MacAddress;
        sensor.Location = request.Location;
        sensor.Category = request.Category;
        sensor.TelemetryType = request.TelemetryType;
        sensor.SamplingIntervalSeconds = request.SamplingIntervalSeconds;
        sensor.NormalMin = request.NormalMin;
        sensor.NormalMax = request.NormalMax;

        await _context.SaveChangesAsync();

        return new(ServiceStatus.Success, Data: sensor);
    }
    /// <summary>
    /// Deletes a sensor based on the provided ID.
    /// If the sensor does not exist, a NotFound response is returned.
    /// If the deletion is successful, a NoContent response is returned.
    /// If an unexpected error occurs, a 500 Internal Server Error response is returned.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<bool> DeleteAsync(Guid id)
    {
        // Check if the sensor exists
        var sensor = await _context.Sensors.FindAsync(id);
        if (sensor is null)
        {
            return false;
        }

        _context.Sensors.Remove(sensor);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<ServiceResult<Sensor>> SetActiveAsync(Guid id, bool isActive)
    {
        var sensor = _context.Sensors.FirstOrDefault(s => s.Id == id);
        if (sensor is null)
        {
            return new(ServiceStatus.NotFound);
        }

        sensor.IsActive = isActive;
        await _context.SaveChangesAsync();

        return new(ServiceStatus.Success, Data: sensor);
    }
}