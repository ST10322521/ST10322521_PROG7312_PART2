using Blazor.Diagrams.Core.Models;
using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Anchors;

namespace SmartX.Client.Components.Topology;

/// <summary>
/// Represents a model for a network link in the topology diagram. This class extends the LinkModel from Blazor.Diagrams and provides additional properties and functionality specific to the SmartX application, such as the Name and Weight properties, which are used to identify and describe the link between nodes in the network topology.
/// 
/// https://github.com/Blazor-Diagrams/Blazor.Diagrams/blob/develop/docs/CustomNodesLinks/Models/DiagramLink.cs
/// </summary>
public sealed class NetworkLinkModel : LinkModel
{
    public NetworkLinkModel(
        string name,
        NodeModel sourceNode,
        NodeModel targetNode,
        double weight) : base(name, sourceNode, targetNode)
    {
        Name = name;
        Weight = weight;

        Labels.Add(new NetworkLinkLabelModel(
            this,
            $"{name}-weight",
            weight.ToString("0.##"),
            0.5));
    }

    public string Name { get; set; }

    public double Weight { get; }
}
