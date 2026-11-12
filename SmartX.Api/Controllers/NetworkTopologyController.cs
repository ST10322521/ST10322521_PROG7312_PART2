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
}