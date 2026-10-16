FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY SmartX.slnx ./
COPY SmartX.Client/SmartX.Client.csproj SmartX.Client/
COPY SmartX.Shared/SmartX.Shared.csproj SmartX.Shared/

RUN dotnet restore SmartX.Client/SmartX.Client.csproj

COPY SmartX.Client/. SmartX.Client/
COPY SmartX.Shared/. SmartX.Shared/

WORKDIR /src/SmartX.Client

RUN dotnet publish SmartX.Client.csproj -c Release -o /app/publish

FROM nginx:stable-alpine3.24 AS runtime

COPY --from=build /app/publish /usr/share/nginx/html

EXPOSE 80

