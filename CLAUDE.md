# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is an AI-powered Order Processing System built with .NET 8. The solution consists of five projects organized as a multi-tier architecture:

- **AI.OrderProcessingSystem.WebApi**: ASP.NET Core MVC web application serving as the main UI/API layer
- **AI.OrderProcessingSystem.Common**: Shared utilities and common code used across projects
- **AI.OrderProcessingSystem.Dal**: Data Access Layer for database operations
- **AI.OrderProcessingSystem.Worker**: Background worker service for async processing
- **AI.OrderProcessingSystem.CronJob**: Scheduled task processor for recurring jobs

## Build and Run Commands

**Note**: The solution file is located at the repository root: `AI.OrderProcessingSystem.sln`. All commands assume you're running from the repository root.

### Build the entire solution
```bash
dotnet build AI.OrderProcessingSystem.sln
```

### Run the web application
```bash
# Run with HTTPS (default profile)
cd AI.OrderProcessingSystem.WebApi
dotnet run --launch-profile https
# Application will be available at https://localhost:7037 and http://localhost:5115

# Run with HTTP only
dotnet run --launch-profile http
# Application will be available at http://localhost:5115
```

### Run other projects
```bash
# Common service
cd AI.OrderProcessingSystem.Common
dotnet run

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
dotnet build AI.OrderProcessingSystem.WebApi/AI.OrderProcessingSystem.WebApi.csproj
dotnet build AI.OrderProcessingSystem.Common/AI.OrderProcessingSystem.Common.csproj
dotnet build AI.OrderProcessingSystem.Dal/AI.OrderProcessingSystem.Dal.csproj
dotnet build AI.OrderProcessingSystem.Worker/AI.OrderProcessingSystem.Worker.csproj
dotnet build AI.OrderProcessingSystem.CronJob/AI.OrderProcessingSystem.CronJob.csproj
```

### Restore dependencies
```bash
dotnet restore AI.OrderProcessingSystem.sln
```

## Architecture Notes

### Project Structure
The solution follows a layered architecture pattern intended for separating concerns:

- **Web Layer** (`AI.OrderProcessingSystem.WebApi`): ASP.NET Core MVC application with Controllers, Models, and Views. Uses default routing pattern `{controller=Home}/{action=Index}/{id?}`. Currently implements a basic Home controller.

- **Common Layer** (`AI.OrderProcessingSystem.Common`): Console application placeholder intended for shared utilities, helpers, constants, and common code used across multiple projects.

- **Data Access Layer** (`AI.OrderProcessingSystem.Dal`): Console application placeholder intended for database repositories, Entity Framework contexts, and data models.

- **Worker Service** (`AI.OrderProcessingSystem.Worker`): Console application placeholder intended for background task processing, message queue consumers, or long-running operations.

- **CronJob Service** (`AI.OrderProcessingSystem.CronJob`): Console application placeholder intended for scheduled/recurring tasks.

### Key Configuration
- **Target Framework**: .NET 8.0
- **Nullable Reference Types**: Enabled across all projects
- **Implicit Usings**: Enabled globally

### Current State
The solution is in early development stage. The WebApi project contains a functional ASP.NET Core MVC template with basic scaffolding. The Common, Dal, Worker, and CronJob projects contain only "Hello, World!" console applications and require proper implementation for their intended purposes.
