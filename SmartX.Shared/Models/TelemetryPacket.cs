using SmartX.Shared.Enums;

namespace SmartX.Shared.Models
{
    public class TelemetryPacket<T>
    {
        public required string DeviceId { get; set; }
        public DateTime Timestamp { get; set; }
        public required TelemetryType TelemetryType { get; set; }
        public required T Value { get; set; }
    }
}