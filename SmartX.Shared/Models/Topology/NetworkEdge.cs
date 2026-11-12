namespace SmartX.Shared.Models.Topology;

public class NetworkEdge
{
    public string SourceNodeId { get; set; } = string.Empty;

    public string TargetNodeId { get; set; } = string.Empty;

    public double Weight { get; set; }

    public bool IsActive { get; set; } = true;
}