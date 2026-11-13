using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using Blazor.Diagrams.Core.Models.Base;

namespace SmartX.Client.Components.Topology;

/// <summary>
/// Represents a model for a network link label in the topology diagram. This class extends the LinkLabelModel from Blazor.Diagrams and provides additional properties and functionality specific to the SmartX application, such as the ShowLabel property, which determines whether the label should be displayed or not.
/// 
/// https://github.com/Blazor-Diagrams/Blazor.Diagrams/blob/develop/docs/CustomNodesLinks/Models/DiagramLinkLabel.cs
/// </summary>
public sealed class NetworkLinkLabelModel : LinkLabelModel
{
    public NetworkLinkLabelModel(
        BaseLinkModel parent,
        string id,
        string content,
        double? distance = null,
        Point? offset = null)
        : base(parent, id, content, distance, offset)
    {
    }

    public NetworkLinkLabelModel(
        BaseLinkModel parent,
        string content,
        double? distance = null,
        Point? offset = null)
        : base(parent, content, distance, offset)
    {
    }

    public bool ShowLabel { get; set; } = true;
}