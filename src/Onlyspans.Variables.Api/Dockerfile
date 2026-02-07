# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution file
COPY Onlyspans.Variables.sln ./

# Copy project file
COPY src/Onlyspans.Variables.Api/Onlyspans.Variables.Api.csproj src/Onlyspans.Variables.Api/

# Restore dependencies
RUN dotnet restore src/Onlyspans.Variables.Api/Onlyspans.Variables.Api.csproj

# Copy source code
COPY src/Onlyspans.Variables.Api/ src/Onlyspans.Variables.Api/

# Build application
WORKDIR /src/src/Onlyspans.Variables.Api
RUN dotnet build -c Release -o /app/build --no-restore

# Publish stage
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish --no-restore /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Expose ports
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD curl --fail http://localhost:8080/health || exit 1

# Labels
LABEL org.opencontainers.image.title="Variables API" \
      org.opencontainers.image.description="Centralized variable storage for release configuration" \
      org.opencontainers.image.vendor="Onlyspans" \
      org.opencontainers.image.source="https://github.com/onlyspans/variables"

# Run application
ENTRYPOINT ["dotnet", "Onlyspans.Variables.Api.dll"]
