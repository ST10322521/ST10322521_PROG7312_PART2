using SmartX.Shared.Enums;

namespace SmartX.Shared.Models;

public class SensorAttachment
{
    public Guid Id { get; set; }

    public Guid SensorId { get; set; }

    public required string OriginalFileName { get; set; }

    public required string StoredFileName { get; set; }

    public required string ContentType { get; set; }

    public long FileSize { get; set; }

    public SensorAttachmentType AttachmentType { get; set; }

    public DateTime UploadedAtUtc { get; set; }
}
