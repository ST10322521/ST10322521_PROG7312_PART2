namespace SmartX.Shared.Models.Topology;

public class NetworkTopology
{
    public List<NetworkNode> Nodes { get; set; } = [];

    public List<NetworkEdge> Edges { get; set; } = [];

    public string? GatewayNodeId { get; set; }

    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}