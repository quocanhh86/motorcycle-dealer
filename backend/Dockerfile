# syntax=docker/dockerfile:1
# ==============================================================================
# Motorcycle Dealer & Loyalty Management Platform — Backend Dockerfile (T-308)
# Multi-stage .NET 10 SDK -> ASP.NET Core 10 non-root container (port 8080)
# ==============================================================================

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy centralized build properties and layer .csproj manifests for cached restore
COPY Directory.Build.props ./
COPY src/MotorcycleDealer.Domain/MotorcycleDealer.Domain.csproj src/MotorcycleDealer.Domain/
COPY src/MotorcycleDealer.Application/MotorcycleDealer.Application.csproj src/MotorcycleDealer.Application/
COPY src/MotorcycleDealer.Infrastructure/MotorcycleDealer.Infrastructure.csproj src/MotorcycleDealer.Infrastructure/
COPY src/MotorcycleDealer.WebApi/MotorcycleDealer.WebApi.csproj src/MotorcycleDealer.WebApi/

RUN dotnet restore src/MotorcycleDealer.WebApi/MotorcycleDealer.WebApi.csproj

# Copy production Clean Architecture source layers, strip any host bin/obj artifacts, and publish Release
COPY src/ ./src/
RUN find src -type d \( -name bin -o -name obj \) -exec rm -rf {} + \
    && dotnet publish src/MotorcycleDealer.WebApi/MotorcycleDealer.WebApi.csproj \
        -c Release \
        -o /app/publish \
        /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Install curl strictly for container healthchecks before dropping root privileges
USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_EnableDiagnostics=0

COPY --from=build --chown=$APP_UID:$APP_UID /app/publish ./

# Enforce non-root execution using built-in .NET container user ($APP_UID = 1654)
USER $APP_UID
EXPOSE 8080

HEALTHCHECK --interval=15s --timeout=5s --start-period=15s --retries=5 \
    CMD curl -fsS http://127.0.0.1:8080/health || exit 1

ENTRYPOINT ["dotnet", "MotorcycleDealer.WebApi.dll"]
