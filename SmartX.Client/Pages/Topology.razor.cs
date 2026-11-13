using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using SmartX.Shared.Models.Topology;
using Blazor.Diagrams;
using Blazor.Diagrams.Core.Models;
using Blazor.Diagrams.Core.Geometry;
using System.Text.Json;
using System.Text.Json.Serialization;
using SmartX.Client.Components.Topology;

namespace SmartX.Client.Pages;

/// <summary>
/// Represents the Topology page in the SmartX client application, which displays the current network topology, including sensors and their statuses. The page retrieves the topology data from the API and provides visual indicators for online/offline sensor statuses.
/// 
/// https://blazor-diagrams.zhaytam.com/documentation/diagram-creation
/// </summary>
public partial class Topology
{
    [Inject]
    private HttpClient Http { get; set; } = null!;

    private NetworkTopology? topology;
    private bool loading = true;
    private string? errorMessage;
    private BlazorDiagram Diagram { get; set; } = null!;
    private string? selectedRouteNodeId;
    private List<string> selectedRoute = [];
    private string? routeError;
    private bool routeLoading;
    /// <summary>
    /// Gets the JSON serializer options used for deserializing the network topology data from the API. The options are configured to be case-insensitive and include a converter for handling enum values as strings.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    /// <summary>
    /// Gets the count of online nodes in the network topology.
    /// </summary>
    private int OnlineNodeCount => topology?.Nodes.Count(n => string.Equals(n.Status, "Online", StringComparison.OrdinalIgnoreCase)) ?? 0;

    /// <summary>
    /// Gets the count of offline nodes in the network topology.
    /// </summary>
    private int OfflineNodeCount => topology?.Nodes.Count(n => string.Equals(n.Status, "Offline", StringComparison.OrdinalIgnoreCase)) ?? 0;
    /// <summary>
    /// Gets the list of edges that form the minimum spanning tree of the network topology. If the topology is null, an empty list is returned. The minimum spanning tree edges are used to calculate the total cost of connecting all nodes in the network with the least total weight.
    /// </summary>
    private IReadOnlyList<NetworkEdge> MinimumSpanningTreeEdges => topology?.MinimumSpanningTreeEdges ?? [];
    /// <summary>
    /// Gets the total cost of the minimum spanning tree by summing the weights of all edges in the minimum spanning tree. If there are no edges, the cost is zero. This value represents the minimum total weight required to connect all nodes in the network topology.
    /// </summary>
    private double MinimumSpanningTreeCost => MinimumSpanningTreeEdges.Sum(edge => edge.Weight);
    /// <summary>
    /// Asynchronously initializes the component by creating a new BlazorDiagram instance and loading the network topology data from the API. The method sets the loading state to true while fetching the data and handles any exceptions that may occur during the HTTP request. If successful, the topology data is stored; otherwise, an error message is set for display. Finally, the loading state is reset to false.
    /// </summary>
    /// <returns></returns>
    protected override async Task OnInitializedAsync()
    {
        Diagram = new BlazorDiagram();
        Diagram.RegisterComponent<NetworkNodeModel, NetworkNodeWidget>();
        Diagram.RegisterComponent<NetworkLinkLabelModel, NetworkLinkLabelWidget>();

        await LoadTopologyAsync();
        BuildDiagram();
    }
    /// <summary>
    /// Asynchronously loads the network topology data from the API. The method sets the loading state to true while fetching the data and handles any exceptions that may occur during the HTTP request. If successful, the topology data is stored; otherwise, an error message is set for display. Finally, the loading state is reset to false.
    /// </summary>
    /// <returns></returns>
    private async Task LoadTopologyAsync()
    {
        loading = true;
        errorMessage = null;

        try
        {
            var response = await Http.GetAsync("api/NetworkTopology");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            topology = JsonSerializer.Deserialize<NetworkTopology>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            errorMessage = $"Unable to load network topology: {ex.Message}";
        }
        finally
        {
            loading = false;
        }
    }
    /// <summary>
    /// Asynchronously refreshes the network topology by reloading the data from the API and rebuilding the diagram representation. The method calls LoadTopologyAsync to fetch the latest topology data and then invokes BuildDiagram to update the visual representation of the network topology in the BlazorDiagram component.
    /// </summary>
    /// <returns></returns>
    private async Task RefreshTopologyAsync()
    {
        await LoadTopologyAsync();
        BuildDiagram();
    }
    /// <summary>
    /// Builds the diagram representation of the network topology using the BlazorDiagram component. The method clears any existing nodes and links, calculates positions for each node (placing the gateway at a fixed position and arranging sensors in a circular layout), and creates diagram nodes and links based on the topology data. Active edges are represented as links between the corresponding nodes in the diagram. 
    /// </summary>
    private void BuildDiagram()
    {
        if (topology is null)
        {
            return;
        }

        Diagram.Nodes.Clear();
        Diagram.Links.Clear();

        var positions = new Dictionary<string, Point>();
        var diagramNodes = new Dictionary<string, NodeModel>(
            StringComparer.OrdinalIgnoreCase);

        var gateway = topology.Nodes.FirstOrDefault(n => n.IsGateway);

        if (gateway is not null)
        {
            positions[gateway.Id] = new Point(450, 50);
        }

        var sensors = topology.Nodes.Where(n => !n.IsGateway).ToList();
        // Arrange sensors in a circular layout around the gateway
        for (var i = 0; i < sensors.Count; i++)
        {
            var angle = 2 * Math.PI * i / Math.Max(sensors.Count, 1);
            var x = 450 + 300 * Math.Cos(angle);
            var y = 300 + 200 * Math.Sin(angle);

            positions[sensors[i].Id] = new Point(x, y);
        }
        // Create diagram nodes for each node in the topology
        foreach (var node in topology.Nodes)
        {
            var diagramNode = Diagram.Nodes.Add(new NetworkNodeModel(node, positions[node.Id]));

            diagramNodes[node.Id] = diagramNode;
        }
        // Create diagram links for each active edge in the topology
        foreach (var edge in topology.Edges.Where(e => e.IsActive))
        {
            if (!diagramNodes.TryGetValue(edge.SourceNodeId, out var sourceNode) || !diagramNodes.TryGetValue(edge.TargetNodeId, out var targetNode))
            {
                continue;
            }

            var link = new NetworkLinkModel(
                $"{edge.SourceNodeId}->{edge.TargetNodeId}",
                sourceNode,
                targetNode,
                edge.Weight);

            Diagram.Links.Add(link);
        }
    }
    /// <summary>
    /// Gets the color associated with a given sensor status. The method maps the status string to a corresponding MudBlazor Color enumeration value, which can be used for visual indicators in the UI. The mapping is case-insensitive and defaults to Color.Default for unrecognized statuses.
    /// </summary>
    /// <param name="status"></param>
    /// <returns></returns>
    private static Color GetStatusColor(string status)
    {
        return status.ToLowerInvariant() switch
        {
            "online" => Color.Success,
            "offline" => Color.Error,
            "warning" => Color.Warning,
            _ => Color.Default
        };
    }

    /// <summary>
    /// Gets the severity level associated with a given sensor status. The method maps the status string to a corresponding Severity enumeration value, which can be used for visual indicators in the UI. The mapping is case-insensitive and defaults to Severity.Info for unrecognized statuses.
    /// </summary>
    /// <param name="status"></param>
    /// <returns></returns>
    private static Severity GetStatusSeverity(string status)
    {
        return status.ToLowerInvariant() switch
        {
            "online" => Severity.Success,
            "offline" => Severity.Error,
            "warning" => Severity.Warning,
            _ => Severity.Info
        };
    }
    /// <summary>
    /// Asynchronously finds a route from the selected sensor node to the gateway node in the network topology. The method checks if a sensor node has been selected and then makes an HTTP GET request to the API endpoint for finding the route. If the request is successful, it deserializes the response into a list of node IDs representing the route. If no route is found or if an error occurs, an appropriate error message is set for display. The method also manages the loading state during the operation.
    /// </summary>
    /// <returns></returns>
    private async Task FindRouteToGatewayAsync()
    {
        selectedRoute = [];
        routeError = null;

        if (string.IsNullOrWhiteSpace(selectedRouteNodeId))
        {
            routeError = "Select a sensor first.";
            return;
        }

        routeLoading = true;
        // Make an HTTP GET request to the API endpoint for finding the route from the selected node to the gateway
        try
        {
            var response = await Http.GetAsync(
                $"api/NetworkTopology/route/{Uri.EscapeDataString(selectedRouteNodeId)}");

            if (!response.IsSuccessStatusCode)
            {
                routeError = $"No route from '{selectedRouteNodeId}' to the gateway was found.";
                return;
            }

            selectedRoute =
                await response.Content.ReadFromJsonAsync<List<string>>() ?? [];
        }
        catch (Exception ex)
        {
            routeError = $"Unable to find route: {ex.Message}";
        }
        finally
        {
            routeLoading = false;
        }
    }
}