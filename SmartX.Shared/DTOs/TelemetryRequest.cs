using SmartX.Shared.Enums;

namespace SmartX.Shared.DTOs;

public class TelemetryRequest
{
    public required string DeviceId { get; set; }
    public DateTime Timestamp { get; set; }
    public TelemetryType TelemetryType { get; set; }
    public required object Value { get; set; }
}