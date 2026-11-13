using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using SmartX.Shared.Models.Topology;

namespace SmartX.Client.Components.Topology;

/// <summary>
/// Represents a model for a network node in the topology diagram. This class extends the NodeModel from Blazor.Diagrams and encapsulates a NetworkNode object, providing additional properties and functionality specific to the SmartX application. The Title property is set based on whether the node is a gateway or a regular sensor, allowing for clear identification in the diagram.
/// 
/// https://blazor-diagrams.zhaytam.com/documentation/nodes-customization-svg
/// </summary>
public class NetworkNodeModel : NodeModel
{
    public NetworkNodeModel(
        NetworkNode networkNode,
        Point? position = null) : base(position)
    {
        NetworkNode = networkNode;
        Title = networkNode.IsGateway ? "Smart-X Gateway" : networkNode.Name;
    }

    public NetworkNode NetworkNode { get; }
}