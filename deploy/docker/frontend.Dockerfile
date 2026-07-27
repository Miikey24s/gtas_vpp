# ─────────────────────────────────────────────────────────────
# GTAS VPP Frontend (Blazor Server) – Multi-stage Docker Build
# Build context: repository root  |  Dockerfile: deploy/docker/frontend.Dockerfile
# ─────────────────────────────────────────────────────────────

# ── Stage 1: Restore + Publish ────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Giữ đúng layout monorepo vì frontend tham chiếu trực tiếp shared DTO chính thức.
COPY Directory.Build.props ./
COPY src/Shared/gtas_vpp_shared.csproj src/Shared/

# Copy FE project file for restore layer caching
COPY src/Frontend/Blazor/gtas_vpp_fe.csproj src/Frontend/Blazor/

# Restore (cached unless .csproj files change; BuildKit mount reuses NuGet cache)
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore src/Frontend/Blazor/gtas_vpp_fe.csproj
    

# Copy all source code
COPY src/Shared/ src/Shared/
COPY src/Frontend/Blazor/ src/Frontend/Blazor/

# Publish in Release mode
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish src/Frontend/Blazor/gtas_vpp_fe.csproj \
    -c Release \
    -o /app/publish

# ── Stage 2: Runtime ──────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# P5/timezone: install tzdata and pin container to Asia/Ho_Chi_Minh so
# server-rendered Blazor timestamps (sidebar clock, notifications)
# display VN time even when deployed to non-VN clouds (e.g. SGP).
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl tzdata \
    && ln -fs /usr/share/zoneinfo/Asia/Ho_Chi_Minh /etc/localtime \
    && dpkg-reconfigure --frontend noninteractive tzdata \
    && rm -rf /var/lib/apt/lists/*

RUN mkdir -p /app/keys && chown app:app /app/keys

# Optimized for performance (Server GC for 8GB RAM VPS)
ENV DOTNET_gcServer=1
# ENV DOTNET_GCConserveMemory=9 (Removed for better CPU throughput)
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production
ENV TZ=Asia/Ho_Chi_Minh

USER app
EXPOSE 5000

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "gtas_vpp_fe.dll"]
