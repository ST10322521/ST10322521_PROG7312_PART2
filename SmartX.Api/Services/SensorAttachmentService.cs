using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SmartX.Api.Data;
using SmartX.Api.Services.Interfaces;
using SmartX.Shared.Enums;
using SmartX.Shared.Models;

namespace SmartX.Api.Services;

public class SensorAttachmentService : ISensorAttachmentService
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024;
    // Define allowed file extensions for each attachment type
    private static readonly Dictionary<SensorAttachmentType, HashSet<string>> AllowedExtensions = new()
    {
        [SensorAttachmentType.Configuration] = new(StringComparer.OrdinalIgnoreCase)
        {
            ".json", ".xml", ".yaml", ".yml", ".txt"
        },

        [SensorAttachmentType.DeploymentPhoto] = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png"
        },

        [SensorAttachmentType.HardwareLog] = new(StringComparer.OrdinalIgnoreCase)
        {
            ".log", ".txt", ".csv"
        }
    };

    private readonly SmartXDbContext _dbContext;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public SensorAttachmentService(
        SmartXDbContext dbContext,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _environment = environment;
        _configuration = configuration;
    }
    /// <summary>
    /// Uploads a sensor attachment asynchronously. The method takes a sensor ID (Guid), an attachment type (SensorAttachmentType), and an IFormFile representing the file to be uploaded. It performs various checks, including verifying the existence of the sensor, validating the attachment type, checking the file size, and ensuring the file extension is allowed for the specified attachment type. If all checks pass, it saves the file to a designated storage location and creates a new SensorAttachment record in the database. The method returns a tuple indicating success or failure, along with any error message and the created SensorAttachment object if successful.
    /// </summary>
    /// <param name="sensorId"></param>
    /// <param name="attachmentType"></param>
    /// <param name="file"></param>
    /// <returns></returns>
    public async Task<(bool Success, string? Error, SensorAttachment? Attachment)> UploadAsync(
        Guid sensorId,
        SensorAttachmentType attachmentType,
        IFormFile file)
    {
        var sensorExists = await _dbContext.Sensors.AnyAsync(sensor => sensor.Id == sensorId);

        if (!sensorExists)
        {
            return (false, "Sensor not found.", null);
        }

        if (!AllowedExtensions.ContainsKey(attachmentType))
        {
            return (false, "Invalid attachment type.", null);
        }

        if (file is null || file.Length == 0)
        {
            return (false, "A non-empty file is required.", null);
        }

        if (file.Length > MaxFileSizeBytes)
        {
            return (false, "File exceeds the maximum allowed size of 10 MB.", null);
        }

        var extension = Path.GetExtension(file.FileName);

        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions[attachmentType].Contains(extension))
        {
            return (
                false,
                $"File type '{extension}' is not allowed for {attachmentType}.",
                null);
        }

        var rootPath = _configuration["AttachmentStorage:RootPath"] ?? "Storage/SensorAttachments";

        var storageRoot = Path.IsPathRooted(rootPath) ? rootPath : Path.Combine(_environment.ContentRootPath, rootPath);

        var sensorDirectory = Path.Combine(storageRoot, sensorId.ToString());

        Directory.CreateDirectory(sensorDirectory);

        var storedFileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var filePath = Path.Combine(sensorDirectory, storedFileName);

        await using (var stream = new FileStream(
            filePath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None))
        {
            await file.CopyToAsync(stream);
        }

        var attachment = new SensorAttachment
        {
            Id = Guid.NewGuid(),
            SensorId = sensorId,
            OriginalFileName = Path.GetFileName(file.FileName),
            StoredFileName = storedFileName,
            ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
            FileSize = file.Length,
            AttachmentType = attachmentType,
            UploadedAtUtc = DateTime.UtcNow
        };

        _dbContext.SensorAttachments.Add(attachment);
        await _dbContext.SaveChangesAsync();

        return (true, null, attachment);
    }
    /// <summary>
    /// Retrieves all sensor attachments for a specific sensor asynchronously. The method takes a sensor ID (Guid) as input and queries the database for all SensorAttachment records associated with that sensor. It orders the results by the upload timestamp in descending order, ensuring that the most recently uploaded attachments appear first. The method returns an IEnumerable of SensorAttachment objects representing the attachments for the specified sensor.
    /// </summary>
    /// <param name="sensorId"></param>
    /// <returns></returns>
    public async Task<IEnumerable<SensorAttachment>> GetForSensorAsync(Guid sensorId)
    {
        return await _dbContext.SensorAttachments
            .Where(attachment => attachment.SensorId == sensorId)
            .OrderByDescending(attachment => attachment.UploadedAtUtc)
            .AsNoTracking()
            .ToListAsync();
    }
    /// <summary>
    /// Retrieves a specific sensor attachment and its file path asynchronously. The method takes a sensor ID (Guid) and an attachment ID (Guid) as input. It queries the database for the SensorAttachment record that matches both the sensor ID and attachment ID. If found, it constructs the file path based on the configured storage root and the sensor's directory. The method returns a tuple containing the SensorAttachment object and the corresponding file path. If the attachment is not found, it returns null values for both the attachment and file path.
    /// </summary>
    /// <param name="sensorId"></param>
    /// <param name="attachmentId"></param>
    /// <returns></returns>
    public async Task<(SensorAttachment? Attachment, string? FilePath)> GetAsync(
        Guid sensorId,
        Guid attachmentId)
    {
        var attachment = await _dbContext.SensorAttachments.AsNoTracking().FirstOrDefaultAsync(a =>
                a.Id == attachmentId &&
                a.SensorId == sensorId);

        if (attachment is null)
        {
            return (null, null);
        }

        var rootPath = _configuration["AttachmentStorage:RootPath"] ?? "Storage/SensorAttachments";

        var storageRoot = Path.IsPathRooted(rootPath) ? rootPath : Path.Combine(_environment.ContentRootPath, rootPath);

        var filePath = Path.Combine(
            storageRoot,
            sensorId.ToString(),
            attachment.StoredFileName);

        return (attachment, filePath);
    }
    /// <summary>
    /// Deletes a specific sensor attachment asynchronously. The method takes a sensor ID (Guid) and an attachment ID (Guid) as input. It first checks if the specified SensorAttachment exists in the database for the given sensor. If found, it constructs the file path based on the configured storage root and the sensor's directory. If the file exists at that path, it deletes the file from the filesystem. Finally, it removes the SensorAttachment record from the database and saves the changes. The method returns a boolean indicating whether the deletion was successful (true) or if the attachment was not found (false).
    /// </summary>
    /// <param name="sensorId"></param>
    /// <param name="attachmentId"></param>
    /// <returns></returns>
    public async Task<bool> DeleteAsync(Guid sensorId, Guid attachmentId)
    {
        var attachment = await _dbContext.SensorAttachments.FirstOrDefaultAsync(a =>
                a.Id == attachmentId &&
                a.SensorId == sensorId);

        if (attachment is null)
        {
            return false;
        }

        var rootPath = _configuration["AttachmentStorage:RootPath"] ?? "Storage/SensorAttachments";

        var storageRoot = Path.IsPathRooted(rootPath) ? rootPath : Path.Combine(_environment.ContentRootPath, rootPath);

        var filePath = Path.Combine(
            storageRoot,
            sensorId.ToString(),
            attachment.StoredFileName);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        _dbContext.SensorAttachments.Remove(attachment);
        await _dbContext.SaveChangesAsync();

        return true;
    }
}
