# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files needed for WebApi
COPY AI.OrderProcessingSystem.WebApi/AI.OrderProcessingSystem.WebApi.csproj AI.OrderProcessingSystem.WebApi/
COPY AI.OrderProcessingSystem.Common/AI.OrderProcessingSystem.Common.csproj AI.OrderProcessingSystem.Common/
COPY AI.OrderProcessingSystem.Dal/AI.OrderProcessingSystem.Dal.csproj AI.OrderProcessingSystem.Dal/

# Restore dependencies for WebApi project only
RUN dotnet restore AI.OrderProcessingSystem.WebApi/AI.OrderProcessingSystem.WebApi.csproj

# Copy all source code
COPY . .

# Build the WebApi project
WORKDIR /src/AI.OrderProcessingSystem.WebApi
RUN dotnet build AI.OrderProcessingSystem.WebApi.csproj -c Release -o /app/build

# Publish
RUN dotnet publish AI.OrderProcessingSystem.WebApi.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy published files
COPY --from=build /app/publish .

# Copy configuration files
COPY Configuration /Configuration

# Expose ports
EXPOSE 80
EXPOSE 443

# Set environment variables
ENV ASPNETCORE_URLS=http://+:80

ENTRYPOINT ["dotnet", "AI.OrderProcessingSystem.WebApi.dll"]
