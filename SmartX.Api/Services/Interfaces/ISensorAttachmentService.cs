using Microsoft.AspNetCore.Http;
using SmartX.Shared.Enums;
using SmartX.Shared.Models;

namespace SmartX.Api.Services.Interfaces;

public interface ISensorAttachmentService
{
    Task<(bool Success, string? Error, SensorAttachment? Attachment)> UploadAsync(
        Guid sensorId,
        SensorAttachmentType attachmentType,
        IFormFile file);

    Task<IEnumerable<SensorAttachment>> GetForSensorAsync(Guid sensorId);

    Task<(SensorAttachment? Attachment, string? FilePath)> GetAsync(Guid sensorId, Guid attachmentId);

    Task<bool> DeleteAsync(Guid sensorId, Guid attachmentId);
}
