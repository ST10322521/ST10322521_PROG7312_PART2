# Smart-X Mesh Ecosystem

Smart-X is a simulated IoT ecosystem developed for the **PROG7312 Programming 3** practical assignment. The application demonstrates an integrated IoT environment where registered sensors publish telemetry to a central API, telemetry is processed using structured data collections, operators can issue real-time commands, historical telemetry can be inspected, and the system provides predictive recommendations based on telemetry and operator activity.

This README documents the implemented functionality for:

- **Part 1 – Task 2: IoT Application**
- **Part 2: Advanced Data Structures and Predictive Action**

The project intentionally does not implement the **Network Topology and Mesh Routing** functionality, as that functionality is reserved for Part 3 / the final PoE scope.

---

# 1. Solution Overview

Smart-X consists of four .NET projects working together:

| Project | Purpose |
|---|---|
| `SmartX.Api` | ASP.NET Core Web API, telemetry processing, command processing, sensor registry, recommendations and persistence |
| `SmartX.Client` | Blazor WebAssembly operator dashboard and user interface |
| `SmartX.Shared` | Shared models, DTOs, enums, generic telemetry structures and validation logic |
| `SmartX.SensorSimulator` | Simulated IoT sensor fleet that discovers registered sensors, generates telemetry and receives commands |

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
                     └───────┬──────────┬───────┘
                             │          │
                           SQLite     SignalR
                             │          │
                             ▼          ▼
                    Sensor Registry   Simulator
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
- Docker / Docker Compose if containerised execution is required

Verify the .NET installation with:

```bash
dotnet --version
```

---

# 3. Building the Solution

From the solution root:

```bash
dotnet restore SmartX.slnx
dotnet build SmartX.slnx
```

The complete solution contains:

```text
SmartX.Api
SmartX.Client
SmartX.Shared
SmartX.SensorSimulator
SmartX.Tests
```

Automated tests can be executed with:

```bash
dotnet test
```

The implemented solution has been tested with the full test suite.

---

# 4. Running the Application

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

Run:

```bash
dotnet run --project SmartX.Client
```

The Blazor WebAssembly client is available through its configured development URL.

## 4.3 Start the Sensor Simulator

With the API running, start the simulated IoT fleet:

```bash
dotnet run --project SmartX.SensorSimulator
```

The simulator discovers registered sensors from the API and creates a simulated device for each registered sensor.

Telemetry is then sent to the API continuously.

---

# 5. Part 1 – Task 2: IoT Application

## 5.1 Sensor Data Ingestion and Telemetry

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

# 6. Sensor Registration

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
Device ID:        ENUM-TEST-001
MAC Address:      AA:BB:CC:DD:EE:99
Location:         Facility A / Zone 1
Category:         Environmental
Telemetry Type:   Temperature
Sampling Interval: 5 seconds
Normal Range:     18 - 28
```

The live sensor registry maintains devices using dictionaries indexed by device ID and MAC address.

---

# 7. Generic Telemetry

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

# 8. Operator Overloading

The shared `SensorReading` model implements operator overloading.

The `+` operator can combine compatible readings, while the `-` operator calculates the difference between readings from the same device and telemetry type.

The subtraction operator is used by the telemetry ingestion service to calculate the change between the current and previous reading.

This calculated change is displayed on the dashboard.

---

# 9. Advanced Arrays and Lists

Smart-X implements both rectangular and jagged arrays through the `TelemetryBatch` model.

## Rectangular array

A rectangular:

```csharp
double[,]
```

array is used for batches where sensors have a consistent number of readings.

## Jagged array

A jagged:

```csharp
double[][]
```

array is used where sensors may have different numbers of readings.

Both implementations are integrated into the live sensor simulator.

The simulator alternates between the rectangular and jagged telemetry batch paths while processing numeric telemetry.

The batch data is converted into:

```csharp
List<SensorReading>
```

before continuing through the normal telemetry pipeline.

This means the array implementations are part of the running application rather than existing only as isolated tests.

---

# 10. Recursive Deployment Validation

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

# 11. Sensor Attachments

Sensors can have supporting files associated with their deployment.

Supported attachment categories include:

- Configuration files
- Deployment photographs
- Hardware logs

Attachments are associated with the relevant sensor and uploaded through the application/API rather than existing as unrelated files.

The API validates supported file types and enforces the configured upload size limit.

Sensor attachments are persisted through the application's database layer.

---

# 12. Dynamic Dashboard

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

# 13. Part 2 – Advanced Data Structures

Part 2 activates the real-time command stream and telemetry history functionality.

The application uses the required advanced data structures in actual runtime services.

---

## 13.1 Queue<T> – FIFO Telemetry Processing

Incoming telemetry is placed into a:

```csharp
Queue<T>
```

The telemetry queue provides **First-In, First-Out (FIFO)** processing.

The background telemetry processor removes telemetry from the queue in the same order in which it was received.

This prevents telemetry messages from being processed in an arbitrary order.

---

## 13.2 PriorityQueue<T> – Critical Alerts

Critical telemetry is handled using:

```csharp
PriorityQueue<T>
```

Critical alerts receive higher priority than normal telemetry processing.

This allows urgent operational events to bypass standard-priority work when required.

The dashboard exposes the resulting critical/error states to the operator.

---

## 13.3 Stack<T> – Command History and Undo

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

The command is sent through the real command pipeline to the simulator.

---

## 13.4 Dictionary<TKey,TValue> – Live Device Registry

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

## 13.5 SortedDictionary<TKey,TValue> – Telemetry History

Historical telemetry is stored using:

```csharp
SortedDictionary<DateTime, SensorReading>
```

for each device.

The timestamp is the key, which maintains readings in chronological order.

The History page retrieves this information from the API and displays:

- Timestamp
- Device
- Telemetry type
- Value

The history functionality is available from the application's active navigation menu.

---

## 13.6 HashSet<T> – Unique Active Error States

The live sensor registry uses:

```csharp
HashSet<string>
```

to maintain active error states.

The HashSet prevents duplicate active error entries.

Examples include:

```text
ENUM-TEST-001:Critical
ENUM-TEST-001:Disconnected
TEST-RELAY-001:Disconnected
```

Disconnected devices are also tracked through this mechanism.

This allows the dashboard to display unique active alerts without repeatedly adding the same error state.

---

# 14. Real-Time Command Stream

The Part 2 command interface allows operators to manually control simulated devices.

The operator can select a device and issue commands such as:

```text
Start
Stop
SetState
```

Commands are sent to the API and placed into the command processing pipeline.

The simulator listens to the SignalR command stream and executes received commands.

The command flow is:

```text
Blazor Commands Page
        │
        │ HTTP
        ▼
CommandsController
        │
        ▼
DeviceCommandQueueService
        │
        ▼
DeviceCommandProcessor
        │
        │ SignalR
        ▼
Sensor Simulator
        │
        ▼
Simulated Device
```

The command history is displayed in the command interface and supports Undo.

---

# 15. Telemetry History

The History page allows operators to select a device and load historical telemetry.

Historical records are retrieved from the API and displayed sequentially by timestamp.

This functionality demonstrates the use of the `SortedDictionary<DateTime, SensorReading>` data structure in the application.

---

# 16. Search and Operational Filtering

The sensor interface supports searching and filtering devices by operational information.

Operators can search using:

- Device ID
- MAC address
- Location

Sensors can also be filtered by category.

Search interactions are recorded by the recommendation service so that operator activity can contribute to predictive recommendations.

---

# 17. Predictive Action and Recommendation Engine

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

Each recommendation includes:

- Device
- Reason
- Priority
- Suggested action

Where appropriate, the operator can directly execute the suggested command or navigate to the command interface for further inspection.

---

# 18. Navigation

The active application navigation includes:

```text
Live Dashboard
Sensors
Deployment Validation
History
Controls
```

The following functionality remains intentionally disabled because it belongs to a later scope:

```text
Network Topology and Mesh Routing
```

---

# 19. Testing

The project contains automated tests in:

```text
SmartX.Tests
```

Run all tests with:

```bash
dotnet test
```

The test suite covers the application's shared models, telemetry structures and core functionality.

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

---

# 20. Verifying the Required Data Structures

The following table maps the Part 2 requirements to their implementation.

| Requirement | Implementation | Runtime Usage |
|---|---|---|
| `Queue<T>` | `TelemetryQueueService` | Incoming telemetry |
| `PriorityQueue<T>` | Critical alert processing | Critical telemetry |
| `Stack<T>` | `DeviceCommandProcessor` | Command history and Undo |
| `Dictionary<TKey,TValue>` | `LiveSensorRegistry` | Live device lookup |
| `SortedDictionary<TKey,TValue>` | `TelemetryHistoryService` | Historical telemetry |
| `HashSet<T>` | `LiveSensorRegistry` | Unique active errors |
| Rectangular array | `TelemetryBatch` | Simulator telemetry |
| Jagged array | `TelemetryBatch` | Simulator telemetry |
| `List<T>` | Telemetry/recommendation/history collections | Application processing |

The structures are integrated into application services and runtime flows rather than being present only as unused classes.

---

# 21. Database

Smart-X uses SQLite for application persistence.

The database stores registered sensors and sensor attachment information.

The live telemetry processing structures are maintained in application memory where appropriate, while registered application data is persisted through the API's database layer.

---

# 22. Project Scope

The implemented scope covers:

### Part 1 – Task 2

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

### Part 2

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

### Not included in this scope

Network topology and mesh routing are intentionally excluded from the current implementation because they are reserved for the later project scope.

---

# 23. Git and Version Control

The project is maintained using Git.

Recommended commands for checking the current repository state:

```bash
git status
git log --oneline --decorate -20
```

The repository should contain the source code for all Smart-X projects, tests, database migrations and documentation required to build and demonstrate the application.

---

# 24. Quick Start

From the solution root:

```bash
dotnet restore SmartX.slnx
dotnet build SmartX.slnx
dotnet test
```

Then start the API:

```bash
dotnet run --project SmartX.Api
```

Start the client:

```bash
dotnet run --project SmartX.Client
```

Finally start the simulator:

```bash
dotnet run --project SmartX.SensorSimulator
```

Once all components are running, the Smart-X dashboard can be used to monitor telemetry, inspect devices, review history, issue commands and view predictive recommendations.