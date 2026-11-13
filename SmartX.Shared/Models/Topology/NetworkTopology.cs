namespace SmartX.Shared.Models.Topology;

public class NetworkTopology
{
    public List<NetworkNode> Nodes { get; set; } = [];

    public List<NetworkEdge> Edges { get; set; } = [];

    public List<NetworkEdge> MinimumSpanningTreeEdges { get; set; } = [];

    public string? GatewayNodeId { get; set; }

    public List<string> GatewayTraversalOrder { get; set; } = [];

    public List<string> ReachableNodeIds { get; set; } = [];

    public List<string> DisconnectedNodeIds { get; set; } = [];

    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}