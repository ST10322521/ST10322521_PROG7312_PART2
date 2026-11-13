using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Services;

namespace SmartX.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NetworkTopologyController : ControllerBase
{
    private readonly NetworkTopologyService _topologyService;

    public NetworkTopologyController(NetworkTopologyService topologyService)
    {
        _topologyService = topologyService;
    }

    /// <summary>
    /// Retrieves the current network topology, including sensors and their statuses. The method queries the database for all sensors, determines their online/offline status based on the live sensor registry, and constructs a NetworkTopology object containing nodes and edges representing the network structure. The topology includes a gateway node and edges connecting the gateway to each sensor. The method returns the constructed NetworkTopology object with the last updated timestamp set to the current UTC time.   
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public async Task<IActionResult> GetTopology()
    {
        var topology = await _topologyService.GetTopologyAsync();

        return Ok(topology);
    }
    /// <summary>
    /// Traverses the network topology starting from a specified node ID. The method performs a traversal of the network graph, identifying all reachable nodes from the given starting node. It returns a list of node IDs representing the traversal order. If the specified starting node is not found or is offline, the method returns a 404 Not Found response with an appropriate message.
    /// </summary>
    /// <param name="startNodeId"></param>
    /// <returns></returns>
    [HttpGet("traverse/{startNodeId}")]
    public async Task<IActionResult> Traverse(string startNodeId)
    {
        var traversal = await _topologyService.TraverseFromAsync(startNodeId);

        if (traversal.Count == 0)
        {
            return NotFound(new
            {
                message = $"Node '{startNodeId}' was not found or is offline."
            });
        }

        return Ok(traversal);
    }
    /// <summary>
    /// Finds a route from a specified starting node to the gateway node in the network topology. The method uses a breadth-first search (BFS) algorithm to determine the shortest path from the start node to the gateway, considering only active nodes and edges. If a valid route is found, it returns a list of node IDs representing the path. If no route exists or if the starting node is not found or offline, it returns a 404 Not Found response with an appropriate message.
    /// </summary>
    /// <param name="startNodeId"></param>
    /// <returns></returns>
    [HttpGet("route/{startNodeId}")]
    public async Task<IActionResult> FindRoute(string startNodeId)
    {
        var route = await _topologyService.FindRouteToGatewayAsync(startNodeId);

        if (route.Count == 0)
        {
            return NotFound(new
            {
                message = $"No route from '{startNodeId}' to the gateway was found."
            });
        }

        return Ok(route);
    }
}