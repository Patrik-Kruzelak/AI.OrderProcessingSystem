# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is an AI-powered Order Processing System built with .NET 8. The solution consists of four projects organized as a multi-tier architecture:

- **AI.OrderProcessingSystem**: ASP.NET Core MVC web application serving as the main UI/API layer
- **AI.OrderProcessingSystem.Dal**: Data Access Layer for database operations
- **AI.OrderProcessingSystem.Worker**: Background worker service for async processing
- **AI.OrderProcessingSystem.CronJob**: Scheduled task processor for recurring jobs

## Build and Run Commands

**Note**: The solution file is located at `AI.OrderProcessingSystem/AI.OrderProcessingSystem.sln`. All commands assume you're running from the repository root.

### Build the entire solution
```bash
dotnet build AI.OrderProcessingSystem/AI.OrderProcessingSystem.sln
```

### Run the web application
```bash
# Run with HTTPS (default profile)
cd AI.OrderProcessingSystem
dotnet run --launch-profile https
# Application will be available at https://localhost:7290 and http://localhost:5143

# Run with HTTP only
dotnet run --launch-profile http
# Application will be available at http://localhost:5143
```

### Run other projects
```bash
# Worker service
cd AI.OrderProcessingSystem.Worker
dotnet run

# CronJob service
cd AI.OrderProcessingSystem.CronJob
dotnet run

# Dal service
cd AI.OrderProcessingSystem.Dal
dotnet run
```

### Build specific project
```bash
dotnet build AI.OrderProcessingSystem/AI.OrderProcessingSystem.csproj
dotnet build AI.OrderProcessingSystem.Dal/AI.OrderProcessingSystem.Dal.csproj
dotnet build AI.OrderProcessingSystem.Worker/AI.OrderProcessingSystem.Worker.csproj
dotnet build AI.OrderProcessingSystem.CronJob/AI.OrderProcessingSystem.CronJob.csproj
```

### Restore dependencies
```bash
dotnet restore AI.OrderProcessingSystem/AI.OrderProcessingSystem.sln
```

## Architecture Notes

### Project Structure
The solution follows a layered architecture pattern intended for separating concerns:

- **Web Layer** (`AI.OrderProcessingSystem`): Standard ASP.NET Core MVC with Controllers, Models, and Views. Uses default routing pattern `{controller=Home}/{action=Index}/{id?}`.

- **Data Access Layer** (`AI.OrderProcessingSystem.Dal`): Currently a console application placeholder - intended for database repositories, Entity Framework contexts, and data models.

- **Worker Service** (`AI.OrderProcessingSystem.Worker`): Currently a console application placeholder - intended for background task processing, message queue consumers, or long-running operations.

- **CronJob Service** (`AI.OrderProcessingSystem.CronJob`): Currently a console application placeholder - intended for scheduled/recurring tasks.

### Key Configuration
- **Target Framework**: .NET 8.0
- **Nullable Reference Types**: Enabled across all projects
- **Implicit Usings**: Enabled globally

### Current State
The projects are currently in skeleton/template form with minimal implementation. The Dal, Worker, and CronJob projects contain only "Hello, World!" console applications and will need proper implementation for their intended purposes.
