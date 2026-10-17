using SmartX.Shared.Enums;

namespace SmartX.Shared.Models
{
    public class Sensor
    {
        public Guid Id { get; set; }
        public required string DeviceId { get; set; }
        public required string MacAddress { get; set; }
        public required string Location { get; set; }
        public required SensorCategory Category { get; set; }
        public required TelemetryType TelemetryType { get; set; }
        public int SamplingIntervalSeconds { get; set; }
        public double NormalMin { get; set; }
        public double NormalMax { get; set; }
        public bool IsActive { get; set; }
    }
}