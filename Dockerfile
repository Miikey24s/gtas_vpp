# ─────────────────────────────────────────────────────────────
# GTAS VPP Frontend (Blazor Server) – Multi-stage Docker Build
# Build context: repo root (.)  |  Dockerfile: gtas_vpp_fe/Dockerfile
# ─────────────────────────────────────────────────────────────

# ── Stage 1: Restore + Publish ────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy shared project (referenced by FE via ../../../gtas_vpp_be/gtas_vpp_shared/)
COPY gtas_vpp_be/gtas_vpp_shared/gtas_vpp_shared.csproj gtas_vpp_be/gtas_vpp_shared/

# Copy FE project file for restore layer caching
COPY gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe.csproj gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/

# Restore (cached unless .csproj files change)
RUN dotnet restore gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe.csproj

# Copy all source code
COPY gtas_vpp_be/gtas_vpp_shared/ gtas_vpp_be/gtas_vpp_shared/
COPY gtas_vpp_fe/ gtas_vpp_fe/

# Publish in Release mode
RUN dotnet publish gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Stage 2: Runtime ──────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Cài đặt curl để hỗ trợ Docker Healthcheck
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Optimized for performance (Server GC for 8GB RAM VPS)
ENV DOTNET_gcServer=1
# ENV DOTNET_GCConserveMemory=9 (Removed for better CPU throughput)
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 5000

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "gtas_vpp_fe.dll"]
