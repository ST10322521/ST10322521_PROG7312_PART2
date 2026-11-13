using SmartX.Shared.Models.Topology;

namespace SmartX.Shared.DataStructures.Graph;
/// <summary>
/// Represents a directed graph where nodes are represented by NetworkNode objects and edges are represented by NetworkEdge objects.
/// </summary>
public class NetworkGraph
{
    private readonly Dictionary<string, List<NetworkEdge>> _adjacencyList =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, NetworkNode> _vertices =
        new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, NetworkNode> Vertices => _vertices;
    /// <summary>
    /// Adds a vertex to the graph. If the vertex already exists, it will be updated with the new node information.
    /// </summary>
    /// <param name="node"></param>
    public void AddVertex(NetworkNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        _vertices[node.Id] = node;

        if (!_adjacencyList.ContainsKey(node.Id))
        {
            _adjacencyList[node.Id] = [];
        }
    }
    /// <summary>
    /// Removes a vertex and all its associated edges from the graph.
    /// </summary>
    /// <param name="nodeId"></param>
    /// <returns></returns>
    public bool RemoveVertex(string nodeId)
    {
        if (!_vertices.Remove(nodeId))
        {
            return false;
        }

        _adjacencyList.Remove(nodeId);

        foreach (var edges in _adjacencyList.Values)
        {
            edges.RemoveAll(edge =>
                string.Equals(edge.SourceNodeId, nodeId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(edge.TargetNodeId, nodeId, StringComparison.OrdinalIgnoreCase));
        }

        return true;
    }
    /// <summary>
    /// Checks if the graph contains a vertex with the specified node ID.
    /// </summary>
    /// <param name="nodeId"></param>
    /// <returns></returns>
    public bool ContainsVertex(string nodeId)
    {
        return _vertices.ContainsKey(nodeId);
    }
    /// <summary>
    /// Adds a directed edge from the source node to the target node with the specified weight and active status. If either the source or target node does not exist, an exception will be thrown. The weight must be non-negative.
    /// </summary>
    /// <param name="sourceNodeId"></param>
    /// <param name="targetNodeId"></param>
    /// <param name="weight"></param>
    /// <param name="isActive"></param>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public void AddEdge(
        string sourceNodeId,
        string targetNodeId,
        double weight,
        bool isActive = true)
    {
        if (!ContainsVertex(sourceNodeId))
        {
            throw new ArgumentException(
                $"Source vertex '{sourceNodeId}' does not exist.",
                nameof(sourceNodeId));
        }

        if (!ContainsVertex(targetNodeId))
        {
            throw new ArgumentException(
                $"Target vertex '{targetNodeId}' does not exist.",
                nameof(targetNodeId));
        }

        if (weight < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(weight),
                "Edge weight cannot be negative.");
        }

        var edge = new NetworkEdge
        {
            SourceNodeId = sourceNodeId,
            TargetNodeId = targetNodeId,
            Weight = weight,
            IsActive = isActive
        };

        _adjacencyList[sourceNodeId].Add(edge);
    }
    /// <summary>
    /// Adds an undirected edge between two nodes by creating two directed edges: one from the source node to the target node and another from the target node to the source node. The weight and active status are the same for both edges. If either node does not exist, an exception will be thrown. The weight must be non-negative.
    /// </summary>
    /// <param name="sourceNodeId"></param>
    /// <param name="targetNodeId"></param>
    /// <param name="weight"></param>
    /// <param name="isActive"></param>
    public void AddUndirectedEdge(
        string sourceNodeId,
        string targetNodeId,
        double weight,
        bool isActive = true)
    {
        AddEdge(sourceNodeId, targetNodeId, weight, isActive);
        AddEdge(targetNodeId, sourceNodeId, weight, isActive);
    }
    /// <summary>
    /// Removes a directed edge from the source node to the target node. If the source node does not exist, the method returns false. If the edge is found and removed, the method returns true; otherwise, it returns false.
    /// </summary>
    /// <param name="sourceNodeId"></param>
    /// <param name="targetNodeId"></param>
    /// <returns></returns>
    public bool RemoveEdge(string sourceNodeId, string targetNodeId)
    {
        if (!_adjacencyList.TryGetValue(sourceNodeId, out var edges))
        {
            return false;
        }

        return edges.RemoveAll(edge =>
            string.Equals(edge.TargetNodeId, targetNodeId, StringComparison.OrdinalIgnoreCase)) > 0;
    }
    /// <summary>
    /// Retrieves a read-only list of edges (neighbors) for the specified node ID. If the node does not exist in the graph, an empty list is returned. The method uses a case-insensitive comparison for node IDs.
    /// </summary>
    /// <param name="nodeId"></param>
    /// <returns></returns>
    public IReadOnlyList<NetworkEdge> GetNeighbours(string nodeId)
    {
        if (!_adjacencyList.TryGetValue(nodeId, out var edges))
        {
            return [];
        }

        return edges.AsReadOnly();
    }
    /// <summary>
    /// Determines whether the graph is connected, meaning there is a path between any two vertices. The method performs a breadth-first search (BFS) starting from an arbitrary vertex and keeps track of visited vertices. If all vertices are visited during the traversal, the graph is considered connected. The method returns true if the graph is connected; otherwise, it returns false.
    /// </summary>
    /// <returns></returns>
    public bool IsConnected()
    {
        if (_vertices.Count <= 1)
        {
            return true;
        }

        var startNode = _vertices.Keys.First();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<string>();

        queue.Enqueue(startNode);
        visited.Add(startNode);

        // Perform BFS to visit all reachable nodes
        while (queue.Count > 0)
        {
            var currentNode = queue.Dequeue();

            foreach (var edge in GetNeighbours(currentNode))
            {
                if (!edge.IsActive || !visited.Add(edge.TargetNodeId))
                {
                    continue;
                }

                queue.Enqueue(edge.TargetNodeId);
            }
        }

        return visited.Count == _vertices.Count;
    }
    /// <summary>
    /// Checks if the node with the specified ID is online. The method looks up the node in the graph's vertices and compares its status to "Online" in a case-insensitive manner. If the node exists and its status is "Online", the method returns true; otherwise, it returns false. If the node does not exist in the graph, the method also returns false.
    /// </summary>
    /// <param name="nodeId"></param>
    /// <returns></returns>
    public bool IsNodeOnline(string nodeId)
    {
        return _vertices.TryGetValue(nodeId, out var node) && string.Equals(node.Status, "Online", StringComparison.OrdinalIgnoreCase);
    }
}