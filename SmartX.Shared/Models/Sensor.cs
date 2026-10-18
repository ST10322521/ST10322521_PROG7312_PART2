using SmartX.Shared.Enums;

namespace SmartX.Shared.Models
{
    public class Sensor
    {
        public Guid Id { get; set; }

        public string DeviceId { get; set; } = string.Empty;
        public string MacAddress { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public SensorCategory Category { get; set; }
        public TelemetryType TelemetryType { get; set; }

        public int SamplingIntervalSeconds { get; set; }
        public double NormalMin { get; set; } = 0;
        public double NormalMax { get; set; } = 0;
        public bool IsActive { get; set; }
    }
}