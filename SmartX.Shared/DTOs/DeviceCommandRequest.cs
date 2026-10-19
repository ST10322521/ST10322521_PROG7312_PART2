namespace SmartX.Shared.DTOs;

public sealed class DeviceCommandRequest
{
    public required string DeviceId { get; set; }

    public required string Command { get; set; }

    public object? Value { get; set; }

    public DateTime Timestamp { get; set; }
}