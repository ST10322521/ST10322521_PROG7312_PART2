using Microsoft.EntityFrameworkCore;
using SmartX.Api.Data;
using SmartX.Shared.DataStructures.Graph;
using SmartX.Shared.Models.Topology;

namespace SmartX.Api.Services;

public class NetworkTopologyService
{
    private readonly SmartXDbContext _context;
    private readonly LiveSensorRegistry _sensorRegistry;
    private readonly TelemetryHistoryService _historyService;

    public NetworkTopologyService(
        SmartXDbContext context,
        LiveSensorRegistry sensorRegistry,
        TelemetryHistoryService historyService)
    {
        _context = context;
        _sensorRegistry = sensorRegistry;
        _historyService = historyService;
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
        var graph = new NetworkGraph();

        var disconnectedDevices = _sensorRegistry.GetDisconnectedDevices(TimeSpan.FromSeconds(10));

        // Add sensor nodes to the topology
        foreach (var sensor in sensors)
        {
            var status = "Unknown";

            var disconnected = disconnectedDevices.Contains(sensor.DeviceId, StringComparer.OrdinalIgnoreCase);

            if (disconnected)
            {
                status = "Offline";
            }
            else
            {
                status = "Online";
            }

            _historyService.TryGetLatestReading(sensor.DeviceId, out var latestReading);

            topology.Nodes.Add(new NetworkNode
            {
                Id = sensor.DeviceId,
                Name = sensor.DeviceId,
                NodeType = "Sensor",
                MacAddress = sensor.MacAddress,
                SensorId = sensor.Id,
                Status = status,
                Location = sensor.Location,
                TelemetryType = latestReading == default ? null : latestReading.TelemetryType,
                TelemetryValue = latestReading == default ? null : latestReading.Value,
                LastTelemetryTimestamp = latestReading == default ? null : latestReading.Timestamp,
                IsGateway = false
            });

            graph.AddVertex(topology.Nodes[^1]);
        }

        var gatewayId = "gateway";

        // Add the gateway node to the topology and connect it to all sensor nodes
        topology.Nodes.Insert(0, new NetworkNode
        {
            Id = gatewayId,
            Name = "Smart-X Gateway",
            NodeType = "Gateway",
            Status = "Online",
            Location = "Server",
            IsGateway = true
        });

        topology.GatewayNodeId = gatewayId;
        graph.AddVertex(topology.Nodes[0]);

        // Add gateway connections to each sensor node.
        foreach (var sensor in sensors)
        {
            graph.AddEdge(
                gatewayId,
                sensor.DeviceId,
                1);
        }

        // Add simulated wireless mesh connections between sensor nodes.
        // Lower connection cost represents a more efficient preferred link.
        for (var i = 0; i < sensors.Count; i++)
        {
            for (var j = i + 1; j < sensors.Count; j++)
            {
                var source = sensors[i];
                var target = sensors[j];

                graph.AddUndirectedEdge(
                    source.DeviceId,
                    target.DeviceId,
                    GetConnectionCost(source.DeviceId, target.DeviceId));
            }
        }

        // Copy the graph edges into the topology DTO used by the API/UI.
        var exportedEdges = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var nodeId in graph.Vertices.Keys)
        {
            foreach (var edge in graph.GetNeighbours(nodeId))
            {
                var edgeKey = string.CompareOrdinal(edge.SourceNodeId, edge.TargetNodeId) < 0
                    ? $"{edge.SourceNodeId}:{edge.TargetNodeId}"
                    : $"{edge.TargetNodeId}:{edge.SourceNodeId}";

                if (exportedEdges.Add(edgeKey))
                {
                    topology.Edges.Add(edge);
                }
            }
        }

        topology.LastUpdated = DateTime.UtcNow;

        return topology;
    }
    /// <summary>
    /// Calculates the connection cost between two devices based on their IDs. The method takes two device IDs as input parameters and generates a combined string by ordering the IDs lexicographically. It then computes the hash code of the combined string, takes its absolute value, and calculates the connection cost as 1 plus the remainder of the hash code divided by 10. This ensures that the connection cost is always a positive integer between 1 and 10. The method is static and private, meaning it can only be called within the NetworkTopologyService class.
    /// </summary>
    /// <param name="sourceId"></param>
    /// <param name="targetId"></param>
    /// <returns></returns>
    private static double GetConnectionCost(string sourceId, string targetId)
    {
        var combined = string.CompareOrdinal(sourceId, targetId) < 0 ? $"{sourceId}:{targetId}" : $"{targetId}:{sourceId}";

        var hash = Math.Abs(combined.GetHashCode());

        return 1 + (hash % 10);
    }
}