using Microsoft.EntityFrameworkCore;
using SmartX.Api.Data;
using SmartX.Shared.Models.Topology;

namespace SmartX.Api.Services;

public class NetworkTopologyService
{
    private readonly SmartXDbContext _context;
    private readonly LiveSensorRegistry _sensorRegistry;

    public NetworkTopologyService(
        SmartXDbContext context,
        LiveSensorRegistry sensorRegistry)
    {
        _context = context;
        _sensorRegistry = sensorRegistry;
    }

    /// <summary>
    /// Asynchronously retrieves the current network topology, including sensors and their statuses. The method queries the database for all sensors, determines their online/offline status based on the live sensor registry, and constructs a NetworkTopology object containing nodes and edges representing the network structure. The topology includes a gateway node and edges connecting the gateway to each sensor. The method returns the constructed NetworkTopology object with the last updated timestamp set to the current UTC time.
    /// </summary>
    /// <returns>A Task representing the asynchronous operation, containing the NetworkTopology object.</returns>
    public async Task<NetworkTopology> GetTopologyAsync()
    {
        var sensors = await _context.Sensors
            .AsNoTracking()
            .ToListAsync();

        var topology = new NetworkTopology();

        // Add sensor nodes to the topology
        foreach (var sensor in sensors)
        {
            var status = "Unknown";

            var disconnected = _sensorRegistry
                .GetDisconnectedDevices(TimeSpan.FromSeconds(10))
                .Contains(sensor.DeviceId, StringComparer.OrdinalIgnoreCase);

            if (disconnected)
            {
                status = "Offline";
            }
            else
            {
                status = "Online";
            }

            topology.Nodes.Add(new NetworkNode
            {
                Id = sensor.DeviceId,
                Name = sensor.DeviceId,
                NodeType = "Sensor",
                MacAddress = sensor.MacAddress,
                SensorId = sensor.Id,
                Status = status,
                Location = sensor.Location
            });
        }

        var gatewayId = "gateway";

        // Add the gateway node to the topology and connect it to all sensor nodes
        topology.Nodes.Insert(0, new NetworkNode
        {
            Id = gatewayId,
            Name = "Smart-X Gateway",
            NodeType = "Gateway",
            Status = "Online",
            Location = "Server"
        });

        topology.GatewayNodeId = gatewayId;

        // Add edges from the gateway to each sensor node
        foreach (var sensor in sensors)
        {
            topology.Edges.Add(new NetworkEdge
            {
                SourceNodeId = gatewayId,
                TargetNodeId = sensor.DeviceId,
                Weight = 1,
                IsActive = true
            });
        }

        topology.LastUpdated = DateTime.UtcNow;

        return topology;
    }
}