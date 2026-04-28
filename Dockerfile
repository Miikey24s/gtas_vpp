# ─────────────────────────────────────────────────────────────
# GTAS VPP Backend – Multi-stage Docker Build
# Build context: repo root (.)  |  Dockerfile: code-be/Dockerfile
# ─────────────────────────────────────────────────────────────

# ── Stage 1: Restore + Publish ────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy shared project (referenced by BE)
COPY gtas_vpp/gtas_vpp_shared/gtas_vpp_shared.csproj          gtas_vpp/gtas_vpp_shared/

# Copy all backend project files for restore layer caching
COPY code-be/gtas_vpp_be.Model/gtas_vpp_be.Model.csproj             code-be/gtas_vpp_be.Model/
COPY code-be/gtas_vpp_be.Service/gtas_vpp_be.Service.csproj         code-be/gtas_vpp_be.Service/
COPY code-be/gtas_vpp_be.AI/gtas_vpp_be.AI.csproj                   code-be/gtas_vpp_be.AI/
COPY code-be/gtas_vpp_be.Migrations/gtas_vpp_be.Migrations.csproj   code-be/gtas_vpp_be.Migrations/
COPY code-be/gtas_vpp_be/gtas_vpp_be.csproj                         code-be/gtas_vpp_be/

# Restore (cached unless .csproj files change)
RUN dotnet restore code-be/gtas_vpp_be/gtas_vpp_be.csproj

# Copy all source code
COPY gtas_vpp/gtas_vpp_shared/ gtas_vpp/gtas_vpp_shared/
COPY code-be/ code-be/

# Publish in Release mode
RUN dotnet publish code-be/gtas_vpp_be/gtas_vpp_be.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Stage 2: Runtime ──────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Low-memory tuning: Workstation GC + conservative memory
ENV DOTNET_gcServer=0
ENV DOTNET_GCConserveMemory=9
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "gtas_vpp_be.dll"]
