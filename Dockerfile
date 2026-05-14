# ─────────────────────────────────────────────────────────────
# GTAS VPP Backend – Multi-stage Docker Build
# Build context: repo root (.)  |  Dockerfile: gtas_vpp_be/Dockerfile
# ─────────────────────────────────────────────────────────────

# ── Stage 1: Restore + Publish ────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy shared project (referenced by BE)
COPY gtas_vpp_be/gtas_vpp_shared/gtas_vpp_shared.csproj          gtas_vpp_be/gtas_vpp_shared/

# Copy all backend project files for restore layer caching
COPY gtas_vpp_be/gtas_vpp_be.Model/gtas_vpp_be.Model.csproj             gtas_vpp_be/gtas_vpp_be.Model/
COPY gtas_vpp_be/gtas_vpp_be.Service/gtas_vpp_be.Service.csproj         gtas_vpp_be/gtas_vpp_be.Service/
COPY gtas_vpp_be/gtas_vpp_be.Migrations/gtas_vpp_be.Migrations.csproj   gtas_vpp_be/gtas_vpp_be.Migrations/
COPY gtas_vpp_be/gtas_vpp_be/gtas_vpp_be.csproj                         gtas_vpp_be/gtas_vpp_be/

# Restore (cached unless .csproj files change; BuildKit mount reuses NuGet cache)
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore gtas_vpp_be/gtas_vpp_be/gtas_vpp_be.csproj

# Copy all source code
COPY gtas_vpp_be/ gtas_vpp_be/

# Publish in Release mode
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish gtas_vpp_be/gtas_vpp_be/gtas_vpp_be.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

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
