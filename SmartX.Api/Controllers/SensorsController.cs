using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartX.Api.Data;
using SmartX.Shared.Models;
using SmartX.Shared.DTOs;
using SmartX.Shared.Validation;

// https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.controllerbase.createdataction?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/dotnet/api/system.data.entity.dbextensions.asnotracking?view=entity-framework-5.0.0

namespace SmartX.Api.Controllers;

[ApiController]
[Route("api/[controller]")]

public class SensorsController : ControllerBase
{
    private readonly SmartXDbContext _context;

    public SensorsController(SmartXDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Creates a new sensor.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<ActionResult<Sensor>> CreateSensor(CreateSensorRequest request)
    {
        var validationErrors = SensorValidation.Validate(request);
        if (validationErrors.Any())
        {
            return BadRequest(new { errors = validationErrors });
        }

        var deviceIdExists = await _context.Sensors.AnyAsync(s => s.DeviceId == request.DeviceId);
        if (deviceIdExists)
        {
            return Conflict(new { error = "A sensor with the same DeviceId already exists." });
        }

        var macAddressExists = await _context.Sensors.AnyAsync(s => s.MacAddress == request.MacAddress);
        if (macAddressExists)
        {
            return Conflict(new { error = "A sensor with the same MacAddress already exists." });
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

        return CreatedAtAction(nameof(GetSensor), new { id = sensor.Id }, sensor);
    }

    /// <summary>
    /// Gets all sensors.
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Sensor>>> GetSensors()
    {
        return await _context.Sensors.AsNoTracking().ToListAsync();
    }

    /// <summary>
    /// Gets a sensor by its unique identifier.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Sensor>> GetSensor(Guid id)
    {
        var sensor = await _context.Sensors.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);

        if (sensor is null)
        {
            return NotFound();
        }

        return sensor;
    }
    /// <summary>
    /// Updates an existing sensor by its unique identifier.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<Sensor>> UpdateSensor(Guid id, CreateSensorRequest request)
    {
        var validationErrors = SensorValidation.Validate(request);
        if (validationErrors.Any())
        {
            return BadRequest(new { errors = validationErrors });
        }

        var sensor = await _context.Sensors.FindAsync(id);
        if (sensor is null)
        {
            return NotFound();
        }

        var deviceIdExists = await _context.Sensors.AnyAsync(s => s.DeviceId == request.DeviceId && s.Id != id);
        if (deviceIdExists)
        {
            return Conflict(new { error = "A sensor with the same DeviceId already exists." });
        }

        var macAddressExists = await _context.Sensors.AnyAsync(s => s.MacAddress == request.MacAddress && s.Id != id);
        if (macAddressExists)
        {
            return Conflict(new { error = "A sensor with the same MacAddress already exists." });
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

        return Ok(sensor);
    }
    /// <summary>
    /// Deletes a sensor by its unique identifier.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteSensor(Guid id)
    {
        var sensor = await _context.Sensors.FindAsync(id);
        if (sensor is null)
        {
            return NotFound();
        }

        _context.Sensors.Remove(sensor);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}