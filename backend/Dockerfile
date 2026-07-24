# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first (layer-cached) by copying only the project files.
COPY src/RecruitmentPlatform.Domain/RecruitmentPlatform.Domain.csproj src/RecruitmentPlatform.Domain/
COPY src/RecruitmentPlatform.Application/RecruitmentPlatform.Application.csproj src/RecruitmentPlatform.Application/
COPY src/RecruitmentPlatform.Infrastructure/RecruitmentPlatform.Infrastructure.csproj src/RecruitmentPlatform.Infrastructure/
COPY src/RecruitmentPlatform.API/RecruitmentPlatform.API.csproj src/RecruitmentPlatform.API/
RUN dotnet restore src/RecruitmentPlatform.API/RecruitmentPlatform.API.csproj

# Copy the rest of the sources and publish.
COPY src/ src/
RUN dotnet publish src/RecruitmentPlatform.API/RecruitmentPlatform.API.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# curl is used by the container healthcheck.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Create writable dirs for uploads/logs and hand ownership to the non-root app user.
RUN mkdir -p /app/uploads /app/logs && chown -R app:app /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 8080
USER app

HEALTHCHECK --interval=15s --timeout=5s --start-period=30s --retries=5 \
    CMD curl -fsS http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "RecruitmentPlatform.API.dll"]
