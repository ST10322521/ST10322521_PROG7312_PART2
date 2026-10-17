namespace SmartX.Shared.Models;

public class DeploymentNode
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public required string NodeType { get; set; }

    public Guid? SensorId { get; set; }

    public List<DeploymentNode> Children { get; set; } = [];
}