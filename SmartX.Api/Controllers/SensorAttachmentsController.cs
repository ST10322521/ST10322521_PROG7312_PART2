using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Services.Interfaces;
using SmartX.Shared.Enums;

namespace SmartX.Api.Controllers;

[ApiController]
[Route("api/sensors/{sensorId:guid}/attachments")]
public class SensorAttachmentsController : ControllerBase
{
    private readonly ISensorAttachmentService _attachmentService;

    public SensorAttachmentsController(ISensorAttachmentService attachmentService)
    {
        _attachmentService = attachmentService;
    }

    [HttpPost]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> Upload(
        Guid sensorId,
        [FromForm] IFormFile file,
        [FromForm] SensorAttachmentType attachmentType)
    {
        var result = await _attachmentService.UploadAsync(
            sensorId,
            attachmentType,
            file);

        if (!result.Success)
        {
            return BadRequest(new { error = result.Error });
        }

        return Created(
            $"/api/sensors/{sensorId}/attachments/{result.Attachment!.Id}",
            result.Attachment);
    }

    [HttpGet]
    public async Task<IActionResult> GetForSensor(Guid sensorId)
    {
        var attachments = await _attachmentService.GetForSensorAsync(sensorId);
        return Ok(attachments);
    }

    [HttpGet("{attachmentId:guid}")]
    public async Task<IActionResult> Get(
        Guid sensorId,
        Guid attachmentId)
    {
        var result = await _attachmentService.GetAsync(
            sensorId,
            attachmentId);

        if (result.Attachment is null)
        {
            return NotFound();
        }

        return Ok(result.Attachment);
    }

    [HttpGet("{attachmentId:guid}/download")]
    public async Task<IActionResult> Download(
        Guid sensorId,
        Guid attachmentId)
    {
        var result = await _attachmentService.GetAsync(
            sensorId,
            attachmentId);

        if (result.Attachment is null || result.FilePath is null)
        {
            return NotFound();
        }

        if (!System.IO.File.Exists(result.FilePath))
        {
            return NotFound();
        }

        var stream = new FileStream(
            result.FilePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);

        return File(
            stream,
            result.Attachment.ContentType,
            result.Attachment.OriginalFileName);
    }

    [HttpDelete("{attachmentId:guid}")]
    public async Task<IActionResult> Delete(
        Guid sensorId,
        Guid attachmentId)
    {
        var deleted = await _attachmentService.DeleteAsync(
            sensorId,
            attachmentId);

        return deleted
            ? NoContent()
            : NotFound();
    }
}
