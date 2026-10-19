FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY SmartX.slnx ./
COPY SmartX.Api/SmartX.Api.csproj SmartX.Api/
COPY SmartX.Shared/SmartX.Shared.csproj SmartX.Shared/

RUN dotnet restore SmartX.Api/SmartX.Api.csproj

COPY SmartX.Api/. SmartX.Api/
COPY SmartX.Shared/. SmartX.Shared/

WORKDIR /src/SmartX.Api

RUN dotnet publish SmartX.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

RUN apt-get update \
    && apt-get install -y curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "SmartX.Api.dll"]

