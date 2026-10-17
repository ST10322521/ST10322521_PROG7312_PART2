
using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Services.Interfaces;
using SmartX.Shared.DTOs;

namespace SmartX.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TelemetryController : ControllerBase
{
    private readonly ITelemetryIngestionService _ingestionService;

    public TelemetryController(
        ITelemetryIngestionService ingestionService)
    {
        _ingestionService = ingestionService;
    }

    [HttpPost]
    public async Task<IActionResult> ReceiveTelemetry(
        TelemetryRequest request)
    {
        try
        {
            var response =
                await _ingestionService.ReceiveTelemetryAsync(request);

            if (response is null)
            {
                return NotFound(new
                {
                    error = "No registered sensor found for the provided DeviceId."
                });
            }

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}