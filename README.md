# AI Order Processing System

A complete REST API system for order processing built with .NET 8, PostgreSQL, and Docker.

## Features

- JWT-based authentication
- CRUD operations for Users, Products, and Orders
- PostgreSQL database with Entity Framework Core
- Swagger/OpenAPI documentation
- Docker containerization
- Integration tests with Testcontainers

## Prerequisites

- .NET 8 SDK
- Docker and Docker Compose
- PostgreSQL 16 (if running locally without Docker)

## Quick Start

### 1. Start the Database and API with Docker Compose

```bash
# From repository root
docker-compose up -d
```

This will:
- Start PostgreSQL on port 5432
- Build and start the WebApi on ports 5115 (HTTP) and 7037 (HTTPS)
- Automatically run migrations and seed data

### 2. Access the API

- **Swagger UI**: http://localhost:5115/swagger
- **Base URL**: http://localhost:5115/api

### 3. Login with Admin Account

**Default Admin Credentials**:
- Email: `admin@orderprocessing.local`
- Password: `Admin@12345`

**Login Request**:
```bash
curl -X POST http://localhost:5115/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@orderprocessing.local",
    "password": "Admin@12345"
  }'
```

**Response**:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2025-12-06T12:00:00Z",
  "user": {
    "id": 1,
    "name": "Administrator",
    "email": "admin@orderprocessing.local"
  }
}
```

### 4. Use the JWT Token

Include the token in subsequent requests:

```bash
curl -X GET http://localhost:5115/api/products \
  -H "Authorization: Bearer YOUR_JWT_TOKEN_HERE"
```

## Manual Setup (Without Docker)

### 1. Start PostgreSQL

Ensure PostgreSQL is running on `localhost:5432` with:
- Database: `orderprocessing`
- Username: `postgres`
- Password: `SecureP@ssw0rd123`

Or update `\Configuration\secrets.json` with your connection string.

### 2. Run Database Migrations

```bash
# From repository root
cd AI.OrderProcessingSystem.Dal
dotnet ef database update --startup-project ../AI.OrderProcessingSystem.WebApi
```

### 3. Run the API

```bash
# From repository root
cd AI.OrderProcessingSystem.WebApi
dotnet run --launch-profile https
```

The API will be available at:
- HTTPS: https://localhost:7037
- HTTP: http://localhost:5115
- Swagger: https://localhost:7037/swagger

## Running Tests

```bash
# From repository root
dotnet test AI.OrderProcessingSystem.WebApi.Tests
```

Tests use Testcontainers to spin up a PostgreSQL instance automatically.

## API Endpoints

### Authentication
- `POST /api/auth/login` - Login and get JWT token

### Users
- `GET /api/users` - Get all users
- `GET /api/users/{id}` - Get user by ID
- `POST /api/users` - Create user
- `PUT /api/users/{id}` - Update user
- `DELETE /api/users/{id}` - Delete user

### Products
- `GET /api/products` - Get all products
- `GET /api/products/{id}` - Get product by ID
- `POST /api/products` - Create product
- `PUT /api/products/{id}` - Update product
- `DELETE /api/products/{id}` - Delete product

### Orders
- `GET /api/orders` - Get all orders
- `GET /api/orders/{id}` - Get order by ID
- `POST /api/orders` - Create order
- `PUT /api/orders/{id}` - Update order status
- `DELETE /api/orders/{id}` - Delete order

All endpoints except `/api/auth/login` require JWT authentication.

## Configuration

Configuration is split into two files in the `\Configuration` directory:

- **secrets.json**: Database credentials, JWT secret, admin password (committed to Git per requirements)
- **instance.json**: Non-sensitive settings like API title, URLs, etc.

## Project Structure

- **AI.OrderProcessingSystem.WebApi**: REST API with controllers and JWT authentication
- **AI.OrderProcessingSystem.Dal**: Data Access Layer with EF Core entities and DbContext
- **AI.OrderProcessingSystem.Common**: Shared DTOs, enums, and constants
- **AI.OrderProcessingSystem.Worker**: Background worker (not used in this phase)
- **AI.OrderProcessingSystem.CronJob**: Scheduled tasks (not used in this phase)
- **AI.OrderProcessingSystem.WebApi.Tests**: Integration tests

## Troubleshooting

**Migration errors**:
```bash
# Reset database
dotnet ef database drop --startup-project ../AI.OrderProcessingSystem.WebApi
dotnet ef database update --startup-project ../AI.OrderProcessingSystem.WebApi
```

**Docker issues**:
```bash
# Rebuild containers
docker-compose down -v
docker-compose up --build
```

**Port conflicts**:
Edit `docker-compose.yml` to change port mappings if 5432, 5115, or 7037 are already in use.

## License

Proprietary - All rights reserved
