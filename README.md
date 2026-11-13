# Smart-X Mesh Ecosystem

Smart-X is a simulated IoT ecosystem developed for the **PROG7312 Programming 3** practical assignment.

The application demonstrates an integrated IoT environment where registered sensors publish telemetry to a central API, telemetry is processed using structured data collections, operators can issue real-time commands, historical telemetry can be inspected, and the system provides predictive recommendations based on telemetry and operator activity.

This README documents the implemented functionality for:

- **Part 1 – Task 2: IoT Application**
- **Part 2: Advanced Data Structures and Predictive Action**
- **Part 3: Advanced Data Structures, Network Topology, Graph Traversal and Mesh Routing**

## Demonstration Video
YouTube: https://youtu.be/MfqcaDcqCeo

---

# 1. Solution Overview

Smart-X consists of four main .NET projects:

| Project | Purpose |
|---|---|
| `SmartX.Api` | ASP.NET Core Web API, telemetry processing, command processing, sensor registry, recommendations and persistence |
| `SmartX.Client` | Blazor WebAssembly operator dashboard and user interface |
| `SmartX.Shared` | Shared models, DTOs, enums, generic telemetry structures and validation logic |
| `SmartX.SensorSimulator` | Simulated IoT sensor fleet that discovers registered sensors, generates telemetry and receives commands |

The solution also contains:

| Project | Purpose |
|---|---|
| `SmartX.Tests` | Automated tests for shared models, telemetry structures and core functionality |

The overall architecture is:

```text
                         ┌──────────────────────────┐
                         │      SmartX.Client       │
                         │  Blazor WebAssembly UI   │
                         └────────────┬─────────────┘
                                      │
                              HTTP / SignalR
                                      │
                                      ▼
                         ┌──────────────────────────┐
                         │        SmartX.Api        │
                         │    ASP.NET Core Web API  │
                         │                          │
                         │ Telemetry Processing     │
                         │ Command Processing       │
                         │ Sensor Registry          │
                         │ Recommendation Engine    │
                         │ History                  │
                         │ SQLite Persistence       │
                         └───────┬──────────┬───────┘
                                 │          │
                               SQLite     SignalR
                                 │          │
                                 ▼          ▼
                         Persistent Data   Simulator
                                             │
                                             ▼
                                  SmartX.SensorSimulator
```

---

# 2. Requirements

The project requires:

- .NET 10 SDK
- Visual Studio or VS Code with C# Dev Kit
- Git
- Docker
- Docker Compose
- Docker and Docker Compose are required only when using the containerised deployment.

Verify the .NET installation:

```bash
dotnet --version
```

Verify Docker:

```bash
docker --version
```

Verify Docker Compose:

```bash
docker compose version
```
---

# 3. Building the Solution Locally

clone: https://github.com/ST10322521/ST10322521_PROG7312_PART2.git

From the solution root:

```bash
dotnet restore SmartX.slnx
dotnet build SmartX.slnx
```

Run the automated tests:

```bash
dotnet test
```

The complete solution contains:

```text
SmartX.Api
SmartX.Client
SmartX.Shared
SmartX.SensorSimulator
SmartX.Tests
```

The solution has been built and tested as a complete application.

---

# 4. Running the Application Locally

Docker is the recommended demonstration method, but the application can also be run directly with the .NET SDK.

## 4.1 Start the API

From the solution root:

```bash
dotnet run --project SmartX.Api
```

The development HTTP API is available at:

```text
http://localhost:5017
```

The SignalR hub is available at:

```text
http://localhost:5017/smartxHub
```

## 4.2 Start the Client

In another terminal:

```bash
dotnet run --project SmartX.Client
```

The Blazor WebAssembly client is available through the development URL displayed by the application.

## 4.3 Start the Sensor Simulator

With the API running, start the simulated IoT fleet:

```bash
dotnet run --project SmartX.SensorSimulator
```

The simulator discovers registered sensors from the API, creates simulated devices dynamically, generates telemetry and receives commands through the Smart-X command pipeline.

---

# 5. Docker Deployment

Smart-X includes Docker support for running the complete application as a multi-container system.

The Docker environment contains:

```text
Browser
   │
   ▼
SmartX Client / Nginx
   │
   ├── /api/ ───────────────► SmartX API
   │
   └── /smartxHub ──────────► SignalR Hub
                                  │
                                  ▼
                         Sensor Simulator
```

Docker Compose runs three services:

| Service | Purpose | Port |
|---|---|---|
| `api` | ASP.NET Core API | `5017` |
| `client` | Blazor WASM + Nginx | `8080` |
| `simulator` | Sensor simulation | Internal |

The browser accesses the complete application through:

```text
http://localhost:8080
```

The API is also exposed for development/testing at:

```text
http://localhost:5017
```

---

# 6. Docker Quick Start

From the solution root:

## 6.1 Validate the Compose configuration

```bash
docker compose config
```

The command should complete successfully without configuration errors.

## 6.2 Build the Docker images

```bash
docker compose build
```

This builds:

```text
smartx-api
smartx-client
smartx-simulator
```

## 6.3 Start the application

```bash
docker compose up -d
```

Docker Compose starts the API first.

The Client and Sensor Simulator wait for the API health check to report that the API is healthy.

## 6.4 Check container status

```bash
docker compose ps
```

The expected state is:

```text
smartx-api         Up ... (healthy)
smartx-client      Up ...
smartx-simulator   Up ...
```

The API must show:

```text
healthy
```

before the other services start.

## 6.5 Open the application

Open:

```text
http://localhost:8080
```

The Smart-X Blazor application should be displayed.

---

# 7. Docker Health Check

The API exposes:

```text
GET /health
```

The endpoint returns a healthy status when the API is running.

Docker uses this endpoint to determine whether the API is ready to accept requests.

The Docker Compose dependency configuration ensures:

```text
API
 │
 │ health check passes
 ▼
Client + Simulator
```

This prevents the sensor simulator from attempting to connect to the API before the API has finished starting.

---

# 8. Docker Persistence

Docker Compose creates persistent volumes for Smart-X application data.

The application uses:

```text
smartx-api-data
```

for the SQLite database and:

```text
smartx-api-attachments
```

for uploaded sensor attachments.

The API container mounts these volumes as:

```text
/app/data
/app/Storage/Attachments
```

This means registered sensors, sensor attachment metadata and uploaded attachment files are not tied only to the lifetime of an individual API container.

To stop the application without removing persistent volumes:

```bash
docker compose down
```

To start it again:

```bash
docker compose up -d
```

To remove containers and the Docker network while retaining named volumes:

```bash
docker compose down
```

To intentionally remove the persistent volumes as well:

```bash
docker compose down -v
```

**Warning:** `docker compose down -v` removes the Smart-X Docker volumes and therefore deletes the persisted Docker database and attachment storage.

---

# 9. Docker Logs

View all service logs:

```bash
docker compose logs
```

View API logs:

```bash
docker compose logs api
```

View Client logs:

```bash
docker compose logs client
```

View Sensor Simulator logs:

```bash
docker compose logs simulator
```

Follow simulator logs in real time:

```bash
docker compose logs -f simulator
```

This is useful for demonstrating that the simulated devices are discovering sensors, generating telemetry and receiving commands.

---

# 10. Rebuilding Docker Images

After changing application source code, rebuild the affected service.

For example:

```bash
docker compose build api
```

or:

```bash
docker compose build client
```

After rebuilding, recreate the containers:

```bash
docker compose down
docker compose up -d
```

To rebuild everything:

```bash
docker compose build
docker compose down
docker compose up -d
```

For a completely clean rebuild:

```bash
docker compose build --no-cache
docker compose down
docker compose up -d
```

---

# 11. Part 1 – Task 2: IoT Application

## 11.1 Sensor Data Ingestion and Telemetry

Smart-X implements a live telemetry pipeline.

The sensor simulator:

1. Discovers registered sensors from the API.
2. Creates simulated devices dynamically.
3. Generates telemetry according to the sensor's telemetry type.
4. Sends telemetry to the API.
5. The API places incoming telemetry into a FIFO queue.
6. A background processor consumes queued telemetry.
7. Telemetry is classified and recorded.
8. Processed telemetry is broadcast to connected clients using SignalR.
9. The Blazor dashboard displays the live telemetry.

The flow is:

```text
Sensor Simulator
      │
      │ POST /api/telemetry
      ▼
Telemetry Queue
      │
      ▼
Background Queue Processor
      │
      ▼
TelemetryIngestionService
      │
      ├── Telemetry classification
      ├── Live sensor registry
      ├── Telemetry history
      └── SignalR broadcast
               │
               ▼
         Blazor Dashboard
```

---

# 12. Sensor Registration

Sensors are registered with the system using identifying and deployment information.

A sensor contains information including:

- Device ID
- MAC address
- Deployment location
- Sensor category
- Telemetry type
- Sampling interval
- Normal minimum value
- Normal maximum value

Example:

```text
Device ID:         ENUM-TEST-001
MAC Address:       AA:BB:CC:DD:EE:99
Location:          Facility A / Zone 1
Category:          Environmental
Telemetry Type:    Temperature
Sampling Interval: 5 seconds
Normal Range:      18 - 28
```

The live sensor registry maintains devices using dictionaries indexed by device ID and MAC address.

---

# 13. Generic Telemetry

Smart-X uses the generic:

```csharp
TelemetryPacket<T>
```

structure to support different telemetry value types without boxing and unboxing.

Examples include:

```text
TelemetryPacket<float>
TelemetryPacket<int>
TelemetryPacket<bool>
```

The telemetry service provides type-specific processing while maintaining a common telemetry model.

This allows the system to support different types of IoT data, including numeric sensor readings and boolean actuator/relay states.

---

# 14. Operator Overloading

The shared `SensorReading` model implements operator overloading.

The `+` operator can combine compatible readings, while the `-` operator calculates the difference between readings from the same device and telemetry type.

The subtraction operator is used by the telemetry ingestion service to calculate the change between the current and previous reading.

The calculated change is displayed on the dashboard.

---

# 15. Advanced Arrays and Lists

Smart-X implements both rectangular and jagged arrays through the `TelemetryBatch` model.

## Rectangular array

A rectangular ```double[,]``` array is used for batches where sensors have a consistent number of readings.

## Jagged array

A jagged ```double[][]``` array is used where sensors may have different numbers of readings.

Both implementations are integrated into the live sensor simulator.

The simulator alternates between the rectangular and jagged telemetry batch paths while processing numeric telemetry.

The batch data is converted into:

```csharp
List<SensorReading>
```

before continuing through the normal telemetry pipeline.

This means the array implementations are part of the running application rather than existing only as isolated tests.

---

# 16. Recursive Deployment Validation

Smart-X supports nested deployment structures using:

```csharp
DeploymentNode
```

A deployment node can contain child deployment nodes.

The validation process recursively traverses the deployment tree and validates:

- Node names
- Node types
- Nested child nodes

The recursive validator reports the full path of invalid nodes so that errors can be located within a nested deployment structure.

The Blazor deployment page sends deployment structures to:

```text
POST /api/deployment/validate
```

and displays the validation result.

---

# 17. Sensor Attachments

Sensors can have supporting files associated with their deployment.

Supported attachment categories include:

- Configuration files
- Deployment photographs
- Hardware logs

Attachments are associated with the relevant sensor and uploaded through the application/API rather than existing as unrelated files.

The API validates supported file types and enforces the configured upload size limit.

Attachment metadata is persisted through the application's database layer, while uploaded files are stored in the configured attachment storage location.

In Docker, attachment storage is persisted using:

```text
smartx-api-attachments
```

mounted at:

```text
/app/Storage/Attachments
```

---

# 18. Dynamic Dashboard

The Smart-X dashboard provides live operational information rather than displaying only static progress indicators.

The dashboard includes:

- Live telemetry
- Device information
- Telemetry status
- Reading changes
- Active alerts
- Disconnected devices
- Suggested actions
- Device inspection actions

Telemetry received from the API is delivered to the client through SignalR so that the dashboard can update as new readings arrive.

---

# 19. Part 2 and Part 3 – Advanced Data Structures

Smart-X uses the required data structures and algorithms not as separate demonstrations, but as functional elements of the application.

Part 2 implements real-time command processing, telemetry history and predictive recommendation capabilities.

Part 3 provides an extension to Smart-X for efficient range queries on historical telemetry and network analysis based on trees, graphs, BFS traversal, heap priority processing and MST algorithms.

Part 3 introduces:

- Historical telemetry storage based on AVL tree
- Range query support for historical telemetry
- Network graph representation
- BFS (breadth-first search) traversal
- Network traversal from gateway or specific sensor
- Route finding
- MST (minimum spanning tree)
- Prim's MST algorithm
- Generic `MinHeap<T>` priority processing
- Network topology visualizer

---

# 20. Queue<T> – FIFO Telemetry Processing

Incoming telemetry is placed into a:

```csharp
Queue<T>
```

The telemetry queue provides **First-In, First-Out (FIFO)** processing.

The background telemetry processor removes telemetry from the queue in the same order in which it was received.

This prevents telemetry messages from being processed in an arbitrary order.

---

# 21. PriorityQueue<T> – Critical Alerts

Critical telemetry is handled using:

```csharp
PriorityQueue<T>
```

Critical alerts are assigned higher priority than normal-priority work.

This allows urgent operational events to be processed ahead of standard-priority telemetry when required.

The resulting critical/error states are exposed to the operator through the dashboard.

---

# 22. Stack<T> – Command History and Undo

Manual commands are maintained using:

```csharp
Stack<DeviceCommandRequest>
```

The stack records commands in Last-In, First-Out order.

The command history supports:

- Start
- Stop
- SetState
- Undo

When the operator selects Undo, the most recent command is removed from the stack and the appropriate inverse command is dispatched.

The inverse command is sent through the normal command pipeline to the simulator.

---

# 23. Dictionary<TKey,TValue> – Live Device Registry

The live sensor registry uses dictionaries to provide efficient device lookup.

Examples include:

```csharp
Dictionary<string, Sensor>
```

indexed by:

- Device ID
- MAC address

This provides approximately O(1) average lookup time for live devices.

The registry is populated from the application's registered sensors and updated as telemetry is received.

---

# 24. AvlTree

Historical telemetry is stored as follows:

```csharp
AvlTree<DateTime, SensorReading>
```

for each device.

```DateTime``` is used as the key, keeping readings ordered by time.

## Purposes

The AVL tree provides ordered in-memory storage for historical sensor readings, providing efficient insertions, lookups, maximum-value searches and range queries.

## Implementation

The generic AvlTree<TKey,TValue> implementation allows for:

- Insertions
- Key lookups
- In-order traversal
- Maximum value search
- Inclusive range queries
- AVL balancing using rotations

Example of insertion:

```csharp
historyTree.Insert(reading.Timestamp, reading);
```

Example of range query:
```csharp
var readings = historyTree
    .Range(from, to)
    .Select(pair => pair.Value)
    .ToList();
```
## AVL Tree Reason

The choice of AVL tree is driven by the need to keep the historical telemetry ordered according to its timestamp and to perform many inserts and timestamp-based queries.
AVL balance ensures that the height of the tree remains logarithmic.

## Smart-X Telemetry

TelemetryHistoryService maintains one AVL tree for each device.

From the History page, operators can choose:

- Sensor/Device
- Type of telemetry
- Start timestamp
- End timestamp

The API queries the appropriate AVL tree and return all readings from that particular period.

The history endpoint is:
```csharp
GET /api/Telemetry/history/{deviceId}/range
```
And this makes the AVL tree a part of the Smart-X system in use, not just for demonstration.

---

# 25. Network Graph

Graphs are employed for representation of the Smart-X sensor network in Part 3.

Implementation is:

```csharp
NetworkGraph
```

with network nodes and weighted edges.

## Purpose

The graph represents relationships between the gateway, sensors and other network devices.

Nodes represent devices and edges represent active network connections.

## Implementation

Nodes:

- Smart-X gateway
- Registered sensors
- Connected network devices

Edges contain:

- Source node
- Target node
- Weight
- Active/inactive state

The graph employs adjacency-based neighbour discovery in order to enable efficient neighbour inspection by graph algorithms.

## Why a Graph?

A graph was chosen because an IoT mesh network consists of entities that have relationships with one another.

Weighted edges allow Smart-X to traverse the network and evaluate its cost.

Adjacency-based storage avoids scanning the entire list of network connections multiple times when searching for neighbours of a particular device.

## Smart-X Usage

`NetworkTopologyService` builds the graph based on the registered sensor network.

The built topology includes:

- Network nodes
- Network edges
- Health/status of nodes
- Gateway information
- Traversal order of the gateway
- Information about disconnected nodes
- Minimum spanning tree edges

---

# 26. Breadth-First Search and Network Routing

The Smart-X framework implements **Breadth-First Search (BFS)** for graph traversal.

DFS is not implemented at the moment and BFS is chosen because the application should process the network layer by layer either from the gateway or from the selected sensor.

## Purpose

The BFS algorithm defines the order of visiting network nodes.

It is important when processing network hops and checking the connectivity from the selected starting point.

## Implementation

The BFS algorithm works on the runtime object `NetworkGraph`.

The gateway node can serve as the starting point for the algorithm, enabling the application to determine the order in which connected sensors are visited.

The following endpoint of the Topology service is available:

```text
GET /api/NetworkTopology/traverse/{startNodeId}
```

## Efficiency

BFS works through all the available nodes and edges in the graph.

As `NetworkGraph` offers adjacency-based neighbor lookup, BFS does not need to scan all network edges.

For adjacency list graphs, the BFS algorithm has the following time complexity:

```text
O(V + E)
```

where:

- `V` = number of vertices/nodes
- `E` = number of edges

## Routing

Route discovery from selected node to gateway is also provided by Smart-X:

```text
GET /api/NetworkTopology/route/{startNodeId}
```

This route discovery runs over Smart-X network graph instead of a demo graph hardcoded.

The Network Topology page shows:

- Starting node
- Traversal
- Gateway
- Connected nodes
- Unconnected nodes
- Status of nodes
- Routing info

---

# 27. Minimum Spanning Tree - Prim's Algorithm

Smart-X uses **Prim's algorithm** to compute the **Minimum Spanning Tree (MST)**.

## Purpose

The MST discovers a minimum-cost set of connections in the network needed to connect all nodes in the network, without forming cycles.

This enables Smart-X to show network optimization based on the generated sensor topology.

## Implementation

Prim's algorithm is implemented in:

```csharp
NetworkGraph
```

Candidates are saved in:

```csharp
MinHeap<MstEdgeCandidate>
```

The algorithm works as follows:

1. Start from the gateway.
2. Add candidate edges to the MinHeap.
3. Extract the candidate with the lowest cost.
4. Add the unvisited target node to the MST.
5. Add eligible edges of that node to the heap.
6. Repeat until no more eligible candidates are left.

## Why Prim's Algorithm?

Prim's algorithm was chosen because it is designed to find an MST in a weighted graph.

The MinHeap offers an efficient way of accessing the lowest-cost candidate edge.

---

# 28. Search and Operational Filtering

The sensor interface supports searching and filtering devices by operational information.

Operators can search using:

- Device ID
- MAC address
- Location

Sensors can also be filtered by category.

Search interactions are recorded by the recommendation service so that operator activity can contribute to predictive recommendations.

## Efficiency

`MinHeap<T>` is used to provide:

```text
Insert: O(log n)
Extract minimum: O(log n)
```

This means that Prim's algorithm uses heap-based priority operations on the candidate edges instead of sorting the entire list of candidates each time.

## Smart-X Implementation

The resulting MST is saved in:

```csharp
NetworkTopology.MinimumSpanningTreeEdges
```

The Network Topology page shows:

- MST source node
- MST destination node
- Weight
- Count of MST edges
- MST total cost

In any connected network with `N` nodes, the MST will have `N - 1` edges.

---

# 29. Predictive Action and Recommendation Engine

Smart-X implements a recommendation engine for Part 2.

The engine analyses:

- Telemetry history
- Active error states
- Disconnected devices
- User searches
- Manually issued commands

The system can identify patterns such as repeated abnormal telemetry or repeated operator searches for a device experiencing an active problem.

Examples of generated recommendations include:

```text
Inspect critical device
Reconnect device
Frequently searched problem device
Repeated telemetry anomaly
```

Recommendations are displayed on the dashboard under:

```text
Suggested Actions
```

Each recommendation includes relevant information such as:

- Device
- Reason
- Priority
- Suggested action

Where appropriate, the operator can directly execute the suggested command or navigate to the command interface for further inspection.

## Predictive Engine Collections

The recommendation engine makes use of the standard collections depending on the nature of the operation to be performed.

| Collection | Purpose | Why chosen | Performance | Smart-X Utilisation |
|---|---|---|---|---|
| `Dictionary<TKey,TValue>` | Keyed state and device queries | Provides fast access to entries by ID. | O(1) average query time | Device and recommendation related states |
| `HashSet<T>` | Membership tests | Ensures that there are no duplicate state entries | O(1) average query time | Errors/disconnected device tracking |
| `List<T>` | Aggregation of ordered results | Basic ordered collection | O(n) search/iteration | Recommendations and result sets |
| `PriorityQueue<T>` | Priority based operations | Automatically prioritises higher-priority operations. | O(log n) enqueue/dequeue | Critical telemetry/error handling |

These collections have been chosen on the basis of the access patterns needed by the application. Keyed accesses employ dictionaries, membership tests use hash sets, ordered results use lists, while priority accesses use priority queues.

Thus the collections form part of the application processing and are not demonstration code.

---

# 30. Navigation

The active application navigation includes:

```text
Live Dashboard
Sensors
Deployment Validation
History
Controls
Network Topology
```

---

# 31. Testing

The project contains automated tests in:

```text
SmartX.Tests
```

Run all tests with:

```bash
dotnet test
```

Runtime testing has also been performed by running the API, client and sensor simulator together.

Important runtime scenarios include:

- Sensor discovery
- Live telemetry ingestion
- Different telemetry value types
- Telemetry anomaly states
- Rectangular telemetry batches
- Jagged telemetry batches
- Historical telemetry
- Critical alerts
- Disconnected devices
- Manual commands
- Command Undo
- SignalR command delivery
- Search interaction tracking
- Predictive recommendations
- Suggested device inspection
- Sensor attachment upload
- Recursive deployment validation

Docker runtime testing additionally verifies:

- API container startup
- API health check
- Client container startup
- Nginx static hosting
- Nginx API reverse proxy
- Nginx SignalR reverse proxy
- Simulator startup after API readiness
- SQLite persistence volume
- Attachment persistence volume
- Complete Smart-X UI through `http://localhost:8080`

---

# 32. Code Examples

Examples below show how the required data structures and algorithms are used in Smart-X.

## 32.1 AVL Tree Insert and Search

```csharp
var historyTree = new AvlTree<DateTime, SensorReading>();

historyTree.Insert(reading.Timestamp, reading);

var readings = historyTree
    .Range(from, to)
    .Select(pair => pair.Value)
    .ToList();
```

The AVL tree maintains telemetry sorted by timestamp and allows searching for the historical records within a certain period.

## 32.2 Network Graph Representation

```csharp
var graph = new NetworkGraph();

graph.AddVertex(node);

graph.AddEdge(new NetworkEdge
{
    SourceNodeId = sourceId,
    TargetNodeId = targetId,
    Weight = weight,
    IsActive = true
});
```

The graph presents Smart-X devices as vertices and connections in the network as weighted edges.

## 32.3 BFS

```csharp
var traversal = graph.BreadthFirstSearch(startNodeId);
```

BFS traverses the actual Smart-X network graph, starting from the chosen starting node.

## 32.4 MinHeap

```csharp
var candidates = new MinHeap<MstEdgeCandidate>();

candidates.Insert(candidate);

if (candidates.TryExtractMin(out var minimum))
{
    // Process the lowest weight candidate edge.
}
```

MinHeap allows priority based extraction of the lowest weight candidate.

## 32.5 Prim's MST

```csharp
var minimumSpanningTree =
    graph.PrimMinimumSpanningTree(gatewayId);
```

Prim's algorithm uses MinHeap to process the candidate edges and create the minimum spanning tree.

## 32.6 Predictive Engine Collections

```csharp
var deviceStates =
    new Dictionary<string, DeviceState>();

var disconnectedDevices =
    new HashSet<string>();

var recommendations =
    new List<Recommendation>();
```

These collections offer the following advantages:

* key-based lookup;
* set membership testing;
* ordered result aggregation.

---

# 33. Database

Smart-X uses SQLite for application persistence.

The database stores registered sensors and sensor attachment metadata.

In Docker, SQLite is stored at:

```text
/app/data/smartx.db
```

and is persisted through the Docker volume:

```text
smartx-api-data
```

The API automatically applies pending Entity Framework Core migrations when it starts.

Live telemetry processing structures such as queues, command history, active error tracking and runtime telemetry history are maintained in application memory where appropriate.

---

# 34. API and SignalR Endpoints

The main API is available locally at:

```text
http://localhost:5017
```

The Docker client communicates with the API through the Nginx reverse proxy.

Important endpoints include:

```text
/health
/api/telemetry
/api/Telemetry/alerts
/api/deployment/validate
/smartxHub
```

The exact API surface can be inspected through the source code and.

---

# 35. Docker Architecture

The Docker environment consists of:

```text
                         Browser
                            │
                            ▼
                  ┌───────────────────┐
                  │   smartx-client   │
                  │      Nginx        │
                  │                   │
                  │ Static Blazor UI  │
                  │ /api reverse proxy│
                  │ SignalR proxy     │
                  └─────────┬─────────┘
                            │
                            ▼
                  ┌───────────────────┐
                  │     smartx-api    │
                  │   ASP.NET Core    │
                  │                   │
                  │ SQLite            │
                  │ Telemetry         │
                  │ Commands          │
                  │ Recommendations   │
                  │ SignalR           │
                  └─────────┬─────────┘
                            │
                            │ Docker network
                            ▼
                  ┌───────────────────┐
                  │ smartx-simulator  │
                  │                   │
                  │ Simulated Sensors │
                  └───────────────────┘

Persistent Docker Volumes:
    smartx-api-data
    smartx-api-attachments
```

The containers communicate through the Docker Compose network:

```text
smartx-network
```

The simulator uses:

```text
SMARTX_API_URL=http://api:8080
```

inside the Docker network.

---

# 36. Local Development vs Docker

Smart-X supports both local development and Docker execution.

## Local development

```text
Client
  │
  ├── http://localhost:5269
  │
  └── API http://localhost:5017
```

The local Client configuration points to:

```text
http://localhost:5017
```

for API communication.

## Docker

```text
Browser
  │
  ▼
http://localhost:8080
  │
  ▼
Nginx
  │
  ├── /api/ ───────► api:8080
  │
  └── /smartxHub ──► api:8080
```

The Docker Client uses the same-origin Nginx proxy rather than attempting to connect directly to the internal Docker hostname.

This allows the same application code to support both local development and containerised execution.

---

# 37. Project Scope

The implemented scope covers:

## Part 1 – Task 2

- Sensor registration
- Telemetry ingestion
- Generic telemetry packets
- Operator overloading
- Advanced arrays
- Lists
- Recursive deployment validation
- Sensor attachments
- Dynamic dashboard
- API integration
- SignalR real-time updates
- Blazor frontend
- Backend processing

## Part 2

- Active navigation
- Real-time command stream
- Command history
- Undo
- Telemetry history
- FIFO queue
- Priority queue
- Stack
- Dictionary
- SortedDictionary
- HashSet
- Search/filter functionality
- User interaction tracking
- Predictive recommendation engine
- Suggested Actions dashboard

## Part 3

- AVL tree for historical telemetry
- Range query of historical telemetry
- Filtering by sensor and telemetry type
- Filter by date/time range
- Network graph creation
- Weighted network edges
- Gateway based network topology
- Breadth-first search
- Network traversal
- Route discovery
- Detection of disconnected nodes
- Minimum spanning tree
- Prim's algorithm
- General MinHeap
- Interactive Network Topology page
- Edge weight and total cost MST display

---

# 38. Git and Version Control

The project is maintained using Git.

Check the current repository state:

```bash
git status
```

View recent commits:

```bash
git log --oneline --decorate -20
```

The repository should contain the source code for all Smart-X projects, tests, database migrations, Docker configuration and documentation required to build and demonstrate the application.

---

# 39. Quick Start – Local Development

From the solution root:

```bash
dotnet restore SmartX.slnx
dotnet build SmartX.slnx
dotnet test
```

Start the API:

```bash
dotnet run --project SmartX.Api
```

Start the client in another terminal:

```bash
dotnet run --project SmartX.Client
```

Start the simulator in another terminal:

```bash
dotnet run --project SmartX.SensorSimulator
```

Once all components are running, open the Client development URL and use the Smart-X dashboard.

---

# 40. Quick Start – Docker

From the solution root:

```bash
docker compose config
docker compose build
docker compose up -d
```

Check the services:

```bash
docker compose ps
```

The API should show:

```text
healthy
```

Open:

```text
http://localhost:8080
```

The complete Smart-X application can then be demonstrated through the browser.

To stop the application:

```bash
docker compose down
```

To start it again:

```bash
docker compose up -d
```

To view live simulator activity:

```bash
docker compose logs -f simulator
```

To stop the application and remove its persistent Docker volumes:

```bash
docker compose down -v
```

Use `docker compose down -v` only when intentionally resetting the Docker application's persisted database and attachment storage.


---

# 42. Final Architecture Summary

Smart-X architecture contains:

```text
Blazor WebAssembly
       │
       │ HTTP / SignalR
       ▼
ASP.NET Core Web API
       │
       ├── Sensor Registration
       ├── Telemetry Ingestion
       ├── FIFO Queue
       ├── Priority Processing
       ├── Live Sensor Registry
       ├── AVL Telemetry History
       ├── Historical Range Queries
       ├── Network Graph
       ├── BFS Traversal
       ├── Route Discovery
       ├── MinHeap
       ├── Prim MST
       ├── Command Processing
       ├── Command Stack / Undo
       ├── Recommendation Engine
       ├── Recursive Validation
       ├── Attachment Management
       └── SQLite Persistence
              │
              ▼
       Sensor Simulator
              │
              └── Simulated IoT Devices
```

Smart-X implementation now encompasses the functionality from integrated Part 1, Part 2 and Part 3.

Part 3 expands the application capabilities from telemetry and command processing to the representation of the IoT environment as a weighted network graph and application of graph algorithms in the context of the application topology.

Historical telemetry is processed by an AVL tree, network graph traversal is performed using BFS, and the minimum spanning tree is generated using Prim's algorithm with the help of a MinHeap.

All these data structures and algorithms are connected with the API and presented through the Blazor operator interface, thus making the final application perform required functionality as part of the integrated runtime.
