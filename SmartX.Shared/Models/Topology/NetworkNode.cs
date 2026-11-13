using SmartX.Shared.Enums;

namespace SmartX.Shared.Models.Topology;

public class NetworkNode
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string NodeType { get; set; } = string.Empty;

    public string? MacAddress { get; set; }

    public Guid? SensorId { get; set; }

    public string Status { get; set; } = "Unknown";

    public string Location { get; set; } = string.Empty;

    public TelemetryType? TelemetryType { get; set; }

    public double? TelemetryValue { get; set; }

    public DateTime? LastTelemetryTimestamp { get; set; }

    public bool IsGateway { get; set; }
}