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
cd AI.OrderProcessingSystem.WebApi
dotnet run --launch-profile https
# Application runs at https://localhost:7037 and http://localhost:5115

# Or with HTTP only
dotnet run --launch-profile http
# Application runs at http://localhost:5115
```

### Run other projects
```bash
# Common console application
cd AI.OrderProcessingSystem.Common
dotnet run

# Worker console application
cd AI.OrderProcessingSystem.Worker
dotnet run

# CronJob console application
cd AI.OrderProcessingSystem.CronJob
dotnet run

# Dal console application
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

### Project Types and Structure

- **AI.OrderProcessingSystem.WebApi** (Sdk: Microsoft.NET.Sdk.Web)
  - ASP.NET Core MVC application with standard template
  - Controllers: HomeController with Index, Privacy, and Error actions
  - Models: ErrorViewModel
  - Middleware: Exception handling, HSTS, HTTPS redirection, static files, routing, authorization
  - Default route: `{controller=Home}/{action=Index}/{id?}`
  - Launch URLs configured in launchSettings.json
  - May reference all projects except: AI.OrderProcessingSystem.Worker, AI.OrderProcessingSystem.CronJob

- **AI.OrderProcessingSystem.Common** (Sdk: Microsoft.NET.Sdk, OutputType: Exe)
  - Console application placeholder
  - Currently contains only "Hello, World!" implementation
  - Intended for shared utilities, helpers, and common code
  - May be referenced by all other projects.
  - Must not reference any other project.

- **AI.OrderProcessingSystem.Dal** (Sdk: Microsoft.NET.Sdk, OutputType: Exe)
  - Console application placeholder
  - Currently contains only "Hello, World!" implementation
  - Intended for database repositories and data models
  - May be referenced by all projects except AI.OrderProcessingSystem.Common.

- **AI.OrderProcessingSystem.Worker** (Sdk: Microsoft.NET.Sdk, OutputType: Exe)
  - Console application placeholder
  - Currently contains only "Hello, World!" implementation
  - Intended for background task processing
  - May reference all projects except: AI.OrderProcessingSystem.WebApi, AI.OrderProcessingSystem.CronJob

- **AI.OrderProcessingSystem.CronJob** (Sdk: Microsoft.NET.Sdk, OutputType: Exe)
  - Console application placeholder
  - Currently contains only "Hello, World!" implementation
  - Intended for scheduled/recurring tasks
  - May reference all projects except: AI.OrderProcessingSystem.WebApi, AI.OrderProcessingSystem.Worker

### Configuration

All projects share these settings:
- **Target Framework**: net8.0
- **Nullable Reference Types**: Enabled
- **Implicit Usings**: Enabled

Sensitive configuration – secrets.json
- \Configuration\secrets.json
- Contains credentials, sensitive keys, tokens, passwords
- Should be added to .gitignore, but for this project I want to keep in github

Non-sensitive configuration – instance.json
- \Configuration\instance.json
- Contains non-sensitive environment parameters like URLs
- Must be committed to the repository

Configuration Loading Requirement, at application startup, the system must:
1. Load secrets.json
2. Load instance.json
3. Merge them into a single runtime configuration object
4. Both files must be validated at startup

### Docker Rules
- A Docker configuration must exist at:
- Path must be \ (root folader) and must be commited into repository

This directory should contain:
- Dockerfile
- docker-compose.yml (if required for DB, queues, Redis, etc.)
- environment loading references for instance.json and secrets.json

### Current Development State

The WebApi project is a functional ASP.NET Core MVC application with basic scaffolding from the default template. The other four projects (Common, Dal, Worker, CronJob) are currently minimal console applications that output "Hello, World!" and need implementation for their intended purposes.
