using SmartX.Shared.DataStructures.Heaps;
using SmartX.Shared.Models.Topology;

namespace SmartX.Shared.DataStructures.Graph;
/// <summary>
/// Represents a directed graph where nodes are represented by NetworkNode objects and edges are represented by NetworkEdge objects.
/// 
/// https://www.geeksforgeeks.org/dsa/prims-minimum-spanning-tree-mst-greedy-algo-5/
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
    /// Performs a breadth-first search (BFS) traversal of the graph starting from the specified node ID. The method returns a list of node IDs in the order they were visited during the traversal. If the starting node does not exist or is offline, an empty list is returned. The BFS algorithm uses a queue to explore nodes level by level and keeps track of visited nodes to avoid cycles.
    /// </summary>
    /// <param name="startNodeId"></param>
    /// <returns></returns>
    public IReadOnlyList<string> BreadthFirstSearch(string startNodeId)
    {
        if (!ContainsVertex(startNodeId))
        {
            return [];
        }

        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var traversalOrder = new List<string>();
        var queue = new Queue<string>();

        if (!IsNodeOnline(startNodeId))
        {
            return [];
        }

        queue.Enqueue(startNodeId);
        visited.Add(startNodeId);
        // Perform BFS traversal
        while (queue.Count > 0)
        {
            var currentNode = queue.Dequeue();
            traversalOrder.Add(currentNode);

            foreach (var edge in GetNeighbours(currentNode))
            {
                if (!edge.IsActive ||
                    !IsNodeOnline(edge.TargetNodeId) ||
                    !visited.Add(edge.TargetNodeId))
                {
                    continue;
                }

                queue.Enqueue(edge.TargetNodeId);
            }
        }

        return traversalOrder;
    }
    /// <summary>
    /// Finds a path from the start node to the target node using breadth-first search (BFS). The method returns a list of node IDs representing the path from the start node to the target node. If either the start or target node does not exist, is offline, or if no path exists between them, an empty list is returned. The BFS algorithm explores nodes level by level and keeps track of visited nodes and their predecessors to reconstruct the path once the target node is reached.
    /// </summary>
    /// <param name="startNodeId"></param>
    /// <param name="targetNodeId"></param>
    /// <returns></returns>
    public IReadOnlyList<string> FindPath(string startNodeId, string targetNodeId)
    {
        if (!ContainsVertex(startNodeId) ||
            !ContainsVertex(targetNodeId) ||
            !IsNodeOnline(startNodeId) ||
            !IsNodeOnline(targetNodeId))
        {
            return [];
        }

        var queue = new Queue<string>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var previous = new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase);

        queue.Enqueue(startNodeId);
        visited.Add(startNodeId);
        // Perform BFS to find the target node
        while (queue.Count > 0)
        {
            var currentNode = queue.Dequeue();

            if (string.Equals(
                currentNode,
                targetNodeId,
                StringComparison.OrdinalIgnoreCase))
            {
                break;
            }
            // Explore neighbors of the current node
            foreach (var edge in GetNeighbours(currentNode))
            {
                if (!edge.IsActive ||
                    !IsNodeOnline(edge.TargetNodeId) ||
                    !visited.Add(edge.TargetNodeId))
                {
                    continue;
                }

                previous[edge.TargetNodeId] = currentNode;
                queue.Enqueue(edge.TargetNodeId);
            }
        }

        if (!visited.Contains(targetNodeId))
        {
            return [];
        }

        var path = new List<string>();
        var current = targetNodeId;
        // Reconstruct the path from target to start using the previous dictionary
        while (true)
        {
            path.Add(current);

            if (string.Equals(
                current,
                startNodeId,
                StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            current = previous[current];
        }

        path.Reverse();
        return path;
    }
    /// <summary>
    /// Finds the minimum spanning tree (MST) of the graph using Prim's algorithm, starting from the specified node ID. The method returns a list of NetworkEdge objects representing the edges included in the MST. If the starting node does not exist or is offline, an empty list is returned. The algorithm maintains a set of visited nodes and a priority queue of candidate edges, selecting the edge with the smallest weight that connects a visited node to an unvisited node until all reachable nodes are included in the MST.
    /// </summary>
    /// <param name="startNodeId"></param>
    /// <returns></returns>
    public IReadOnlyList<NetworkEdge> PrimMinimumSpanningTree(string startNodeId)
    {
        if (!ContainsVertex(startNodeId) || !IsNodeOnline(startNodeId))
        {
            return [];
        }

        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var minimumSpanningTree = new List<NetworkEdge>();
        var candidates = new MinHeap<MstEdgeCandidate>();

        visited.Add(startNodeId);
        // Add all edges from the starting node to the candidate list
        foreach (var edge in GetNeighbours(startNodeId))
        {
            if (edge.IsActive && IsNodeOnline(edge.TargetNodeId))
            {
                candidates.Insert(new MstEdgeCandidate
                {
                    SourceNodeId = edge.SourceNodeId,
                    TargetNodeId = edge.TargetNodeId,
                    Weight = edge.Weight
                });
            }
        }
        // Continue adding edges to the MST until all reachable nodes are included
        while (candidates.TryExtractMin(out var candidate))
        {
            if (!visited.Add(candidate.TargetNodeId))
            {
                continue;
            }

            minimumSpanningTree.Add(new NetworkEdge
            {
                SourceNodeId = candidate.SourceNodeId,
                TargetNodeId = candidate.TargetNodeId,
                Weight = candidate.Weight,
                IsActive = true
            });
            // Add all edges from the newly added node to the candidate list
            foreach (var edge in GetNeighbours(candidate.TargetNodeId))
            {
                if (edge.IsActive &&
                    IsNodeOnline(edge.TargetNodeId) &&
                    !visited.Contains(edge.TargetNodeId))
                {
                    candidates.Insert(new MstEdgeCandidate
                    {
                        SourceNodeId = edge.SourceNodeId,
                        TargetNodeId = edge.TargetNodeId,
                        Weight = edge.Weight
                    });
                }
            }
        }

        return minimumSpanningTree;
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
    /// <summary>
    /// Represents a candidate edge for inclusion in the minimum spanning tree (MST) of the graph. The class implements the IComparable interface to allow comparison of edges based on their weights, enabling the use of priority queues or sorting algorithms to efficiently select the next edge to add to the MST. Each candidate edge contains the source node ID, target node ID, and weight, which are required properties for constructing the MST.
    /// </summary>
    private sealed class MstEdgeCandidate : IComparable<MstEdgeCandidate>
    {
        public required string SourceNodeId { get; init; }

        public required string TargetNodeId { get; init; }

        public required double Weight { get; init; }
        /// <summary>
        /// Compares the current MstEdgeCandidate instance with another instance based on their weights. The method returns a negative integer if the current instance has a smaller weight, zero if the weights are equal, and a positive integer if the current instance has a larger weight. If the other instance is null, the current instance is considered to be less than the null instance, and a negative integer is returned. This comparison is used to determine the order of edges when constructing the minimum spanning tree.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(MstEdgeCandidate? other)
        {
            if (other is null)
            {
                return -1;
            }

            return Weight.CompareTo(other.Weight);
        }
    }
}