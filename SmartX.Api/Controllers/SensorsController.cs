using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartX.Api.Data;
using SmartX.Shared.Models;
using SmartX.Shared.DTOs;

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
}