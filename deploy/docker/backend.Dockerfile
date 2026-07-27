# ─────────────────────────────────────────────────────────────
# GTAS VPP Backend – Multi-stage Docker Build
# Build context: repository root  |  Dockerfile: deploy/docker/backend.Dockerfile
# ─────────────────────────────────────────────────────────────

# ── Stage 1: Restore + Publish ────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy build configuration and project files for restore layer caching
COPY Directory.Build.props ./
COPY src/Shared/gtas_vpp_shared.csproj src/Shared/

COPY src/Backend/Domain/gtas_vpp_be.Model.csproj src/Backend/Domain/
COPY src/Backend/Application/gtas_vpp_be.Service.csproj src/Backend/Application/
COPY src/Backend/Migrations/gtas_vpp_be.Migrations.csproj src/Backend/Migrations/
COPY src/Backend/Api/gtas_vpp_be.csproj src/Backend/Api/

# Restore (cached unless .csproj files change; BuildKit mount reuses NuGet cache)
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore src/Backend/Api/gtas_vpp_be.csproj

# Copy all source code
COPY src/ src/

# Publish in Release mode
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish src/Backend/Api/gtas_vpp_be.csproj \
    -c Release \
    -o /app/publish

# ── Stage 2: Runtime ──────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Cài đặt curl để hỗ trợ Docker Healthcheck + tzdata để pin container về VN time
# (P5/timezone: app already uses IDateTimeProvider for business timestamps,
# but TZ env makes Serilog logs, GETDATE() defaults, and *nix tools display
# Asia/Ho_Chi_Minh consistently — important when deploying to non-VN clouds
# such as Singapore/Tokyo/etc.)
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl tzdata \
    && ln -fs /usr/share/zoneinfo/Asia/Ho_Chi_Minh /etc/localtime \
    && dpkg-reconfigure --frontend noninteractive tzdata \
    && mkdir -p /app/logs \
    && chown app:app /app/logs \
    && rm -rf /var/lib/apt/lists/*

# Optimized for performance (Server GC for 8GB RAM VPS)
ENV DOTNET_gcServer=1
# ENV DOTNET_GCConserveMemory=9 (Removed for better CPU throughput)
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV TZ=Asia/Ho_Chi_Minh

USER app
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "gtas_vpp_be.dll"]
