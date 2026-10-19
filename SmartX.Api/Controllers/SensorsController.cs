using Microsoft.AspNetCore.Mvc;
using SmartX.Shared.Models;
using SmartX.Shared.DTOs;
using SmartX.Api.Results;
using SmartX.Api.Services.Interfaces;

// https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.controllerbase.createdataction?view=aspnetcore-10.0

namespace SmartX.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SensorsController : ControllerBase
{
    private readonly ISensorService _sensorService;

    public SensorsController(ISensorService sensorService)
    {
        _sensorService = sensorService;
    }
    /// <summary>
    /// Creates a new sensor based on the provided request data.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<ActionResult<Sensor>> CreateSensor(CreateSensorRequest request)
    {
        // Validate the request
        var result = await _sensorService.CreateAsync(request);
        if (result.Status == ServiceStatus.ValidationFailure)
        {
            return BadRequest(new { errors = result.Errors });
        }
        if (result.Status == ServiceStatus.Conflict)
        {
            return Conflict(new { error = result.Error });
        }

        return CreatedAtAction(nameof(GetSensor), new { id = result.Data!.Id }, result.Data);
    }
    /// <summary>
    /// Retrieves all sensors from the database.
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Sensor>>> GetSensors()
    {
        var sensors = await _sensorService.GetAllAsync();
        return Ok(sensors);
    }
    /// <summary>
    /// Retrieves a specific sensor by its unique identifier (ID).
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Sensor>> GetSensor(Guid id)
    {
        // Check if the sensor exists
        var sensor = await _sensorService.GetByIdAsync(id);
        if (sensor is null)
        {
            return NotFound();
        }

        return Ok(sensor);
    }
    /// <summary>
    /// Updates an existing sensor based on the provided ID and request data.
    /// If the sensor does not exist, a NotFound response is returned.
    /// If the request data is invalid, a BadRequest response is returned.
    /// If there is a conflict with existing sensors (e.g., duplicate DeviceId or MacAddress), a Conflict response is returned.
    /// If the update is successful, an Ok response with the updated sensor data is returned.
    /// If an unexpected error occurs, a 500 Internal Server Error response is returned.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<Sensor>> UpdateSensor(Guid id, CreateSensorRequest request)
    {
        // Validate the request
        var result = await _sensorService.UpdateAsync(id, request);
        return result.Status switch
        {
            ServiceStatus.ValidationFailure => BadRequest(new { errors = result.Errors }),

            ServiceStatus.NotFound => NotFound(),

            ServiceStatus.Conflict => Conflict(new { error = result.Error }),

            ServiceStatus.Success => Ok(result.Data),

            _ => StatusCode(500)
        };
    }
    /// <summary>
    /// Deletes a sensor based on the provided ID.
    /// If the sensor does not exist, a NotFound response is returned.
    /// If the deletion is successful, a NoContent response is returned.
    /// If an unexpected error occurs, a 500 Internal Server Error response is returned.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteSensor(Guid id)
    {
        var deleted = await _sensorService.DeleteAsync(id);

        return deleted ? NoContent() : NotFound();
    }
}