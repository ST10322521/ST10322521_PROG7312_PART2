FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY SmartX.slnx ./
COPY SmartX.SensorSimulator/SmartX.SensorSimulator.csproj SmartX.SensorSimulator/
COPY SmartX.Shared/SmartX.Shared.csproj SmartX.Shared/

RUN dotnet restore SmartX.SensorSimulator/SmartX.SensorSimulator.csproj

COPY SmartX.SensorSimulator/. SmartX.SensorSimulator/
COPY SmartX.Shared/. SmartX.Shared/

WORKDIR /src/SmartX.SensorSimulator

RUN dotnet publish SmartX.SensorSimulator.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime

WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "SmartX.SensorSimulator.dll"]