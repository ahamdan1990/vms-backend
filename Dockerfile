# Use the official .NET 8 runtime as the base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Create non-root user for security
RUN addgroup --system --gid 1001 vmsuser
RUN adduser --system --uid 1001 --gid 1001 vmsuser

# Create directories and set permissions
RUN mkdir -p /app/logs /app/uploads /app/temp
RUN chown -R vmsuser:vmsuser /app

# Use the official .NET 8 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["VisitorManagementSystem.Api.csproj", "."]
RUN dotnet restore "VisitorManagementSystem.Api.csproj"

# Copy source code and build
COPY . .
WORKDIR "/src"
RUN dotnet build "VisitorManagementSystem.Api.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "VisitorManagementSystem.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Copy templates and static files
COPY --from=build /src/Templates ./Templates

# Set proper file permissions
USER vmsuser

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:80/health || exit 1

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

ENTRYPOINT ["dotnet", "VisitorManagementSystem.Api.dll"]