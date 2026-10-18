using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Forms;
using SmartX.Shared.Enums;
using SmartX.Shared.Models;

// https://www.telerik.com/blogs/blazor-basics-uploading-files-blazor-server-web-applications

namespace SmartX.Client.Services;

public class SensorAttachmentService
{
    private readonly HttpClient _httpClient;

    public SensorAttachmentService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<SensorAttachment>> GetForSensorAsync(Guid sensorId)
    {
        return await _httpClient.GetFromJsonAsync<List<SensorAttachment>>(
            $"api/sensors/{sensorId}/attachments") ?? new List<SensorAttachment>();
    }

    public async Task UploadAsync(
        Guid sensorId,
        SensorAttachmentType attachmentType,
        IBrowserFile file)
    {
        using var content = new MultipartFormDataContent();

        var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);

        using var streamContent = new StreamContent(stream);

        streamContent.Headers.ContentType =
            new MediaTypeHeaderValue(file.ContentType);

        content.Add(streamContent, "file", file.Name);
        content.Add(
            new StringContent(attachmentType.ToString()),
            "attachmentType");

        var response = await _httpClient.PostAsync(
            $"api/sensors/{sensorId}/attachments",
            content);

        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteAsync(Guid sensorId, Guid attachmentId)
    {
        var response = await _httpClient.DeleteAsync(
            $"api/sensors/{sensorId}/attachments/{attachmentId}");

        response.EnsureSuccessStatusCode();
    }

    public string GetDownloadUrl(Guid sensorId, Guid attachmentId)
    {
        return $"{_httpClient.BaseAddress}api/sensors/{sensorId}/attachments/{attachmentId}/download";
    }
}
