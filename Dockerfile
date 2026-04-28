# ─────────────────────────────────────────────────────────────
# GTAS VPP Frontend (Blazor Server) – Multi-stage Docker Build
# Build context: repo root (.)  |  Dockerfile: code-fe/Dockerfile
# ─────────────────────────────────────────────────────────────

# ── Stage 1: Restore + Publish ────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy shared project (referenced by FE)
COPY gtas_vpp/gtas_vpp_shared/gtas_vpp_shared.csproj gtas_vpp/gtas_vpp_shared/

# Copy FE project file for restore layer caching
COPY code-fe/gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe.csproj code-fe/gtas_vpp_fe/gtas_vpp_fe/

# Restore (cached unless .csproj files change)
RUN dotnet restore code-fe/gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe.csproj

# Copy all source code
COPY gtas_vpp/gtas_vpp_shared/ gtas_vpp/gtas_vpp_shared/
COPY code-fe/ code-fe/

# Publish in Release mode
RUN dotnet publish code-fe/gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Stage 2: Runtime ──────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Low-memory tuning: Workstation GC
ENV DOTNET_gcServer=0
ENV DOTNET_GCConserveMemory=9
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 5000

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "gtas_vpp_fe.dll"]
