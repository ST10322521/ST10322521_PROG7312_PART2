# Smart-X Mesh Ecosystem

Smart-X is a simulated IoT ecosystem developed for the PROG7312 module. The system represents a distributed IoT environment in which sensors register with a central API and publish telemetry.

## Solution Structure

The Smart-X solution is divided into four projects:
- SmartX.Api - ASP.NET Core Web API and central application gateway.
- SmartX.Client - Blazor WebAssembly frontend.
- SmartX.Shared - Shared models, DTOs, enums and validation definitions.
- SmartX.SensorSimulator - External simulated IoT device environment.

## Requirements

- .NET 10 SDK
- Visual Studio or VS Code with C# Dev Kit 
- Git
- Docker and Docker Compose

## Build 

From the solution root:
```
dotnet restore SmartX.slnx
dotnet build SmartX.slnx
```

## Local Development

The API currently uses the ASP.NET Core Development environment when launched through its development launch profiles.

The API development HTTP endpoint is: 
```
http://localhost:5017
```

## Architecture
```
SmartX.Client
      │
      │ HTTP / SignalR
      ▼
SmartX.Api
      │
      ├── Application logic
      │
      └── Persistence
              │
              ▼
           SQLite

SmartX.SensorSimulator
      │
      │ HTTP
      ▼
SmartX.Api
      │
      ▼
     SQLite
```

The simulator represents external IoT devices. It discovers registered sensors through the API and dynamically creates or removes simulated devices as the registered sensor population changes.