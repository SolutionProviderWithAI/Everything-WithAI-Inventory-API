# Multi-stage Dockerfile for ASP.NET Core Web API (.NET 10)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy solution and project files
COPY InventoryOrderSystem.slnx ./
COPY src/InventoryOrderSystem.Core/InventoryOrderSystem.Core.csproj src/InventoryOrderSystem.Core/
COPY src/InventoryOrderSystem.Infrastructure/InventoryOrderSystem.Infrastructure.csproj src/InventoryOrderSystem.Infrastructure/
COPY src/InventoryOrderSystem.Api/InventoryOrderSystem.Api.csproj src/InventoryOrderSystem.Api/

# Restore dependencies
RUN dotnet restore src/InventoryOrderSystem.Api/InventoryOrderSystem.Api.csproj

# Copy entire source tree and build
COPY src/ src/
WORKDIR /app/src/InventoryOrderSystem.Api
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DatabaseProvider=PostgreSQL

ENTRYPOINT ["dotnet", "InventoryOrderSystem.Api.dll"]
