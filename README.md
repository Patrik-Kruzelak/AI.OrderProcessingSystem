# AI Order Processing System

A complete event-driven order processing system built with .NET 8, PostgreSQL, RabbitMQ, and Docker.

## Features

- JWT-based authentication
- CRUD operations for Users, Products, and Orders
- **Event-Driven Architecture** with MassTransit and RabbitMQ
- **Asynchronous Order Processing** with background workers
- **Automated Order Expiry** with scheduled cron jobs
- **Email Notifications** for completed orders
- PostgreSQL database with Entity Framework Core
- Swagger/OpenAPI documentation
- Docker containerization with multi-service orchestration
- Integration tests with Testcontainers

## Prerequisites

- .NET 8 SDK
- Docker and Docker Compose
- PostgreSQL 16 (if running locally without Docker)

## Architecture Overview

The system uses an event-driven architecture with the following components:

1. **WebApi**: REST API that publishes `OrderCreatedEvent` when orders are created
2. **Worker**: Consumes events from RabbitMQ and processes orders asynchronously
   - Consumes `OrderCreatedEvent` → processes payment → publishes `OrderCompletedEvent` or leaves in "processing"
   - Consumes `OrderCompletedEvent` → sends email notification
   - Consumes `OrderExpiredEvent` → saves expiry notification
3. **CronJob**: Scheduled service that checks for expired orders every 60 seconds
   - Finds orders stuck in "pending" or "processing" for >10 minutes
   - Updates status to "expired"
   - Publishes `OrderExpiredEvent`
4. **RabbitMQ**: Message broker for event distribution
5. **PostgreSQL**: Relational database for persistent storage

## Quick Start

### 1. Start All Services with Docker Compose

```bash
# From repository root
docker-compose up -d
```

This will start:
- **PostgreSQL** on port 5432
- **RabbitMQ** on ports 5672 (AMQP) and 15672 (Management UI)
- **WebApi** on ports 5115 (HTTP) and 7037 (HTTPS)
- **Worker** (background service)
- **CronJob** (scheduled task service)
- Automatically run migrations and seed data

### 2. Access the Services

- **Swagger UI**: http://localhost:5115/swagger
- **API Base URL**: http://localhost:5115/api
- **RabbitMQ Management UI**: http://localhost:15672
  - Username: `guest`
  - Password: `guest`

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

- **secrets.json**: Database credentials, JWT secret, RabbitMQ credentials, admin password (committed to Git per requirements)
- **instance.json**: Non-sensitive settings like:
  - API title and version
  - RabbitMQ host and port
  - Event processing settings (delays, success rates, expiry thresholds)

## Event Flow

### Order Creation
1. User creates order via `POST /api/orders`
2. WebApi saves order with status "pending"
3. WebApi publishes `OrderCreatedEvent` to RabbitMQ
4. Worker consumes event and processes payment (simulated 5 second delay)
5. **On Success (50% probability)**:
   - Worker updates order status to "completed"
   - Worker publishes `OrderCompletedEvent`
   - Worker consumes `OrderCompletedEvent` and sends email notification
6. **On Failure (50% probability)**:
   - Order remains in "processing" status
   - Will be expired by CronJob after 10 minutes

### Order Expiry
1. CronJob runs every 60 seconds
2. Finds orders in "pending" or "processing" status older than 10 minutes
3. Updates order status to "expired"
4. Publishes `OrderExpiredEvent` to RabbitMQ
5. Worker consumes event and saves notification (no email sent)

## Project Structure

- **AI.OrderProcessingSystem.WebApi**: REST API with controllers and JWT authentication
- **AI.OrderProcessingSystem.Dal**: Data Access Layer with EF Core entities and DbContext
- **AI.OrderProcessingSystem.Common**: Shared utilities, events, and configuration
  - Events: `OrderCreatedEvent`, `OrderCompletedEvent`, `OrderExpiredEvent`
  - Abstractions: `IEventPublisher`
  - Configuration: Settings models
- **AI.OrderProcessingSystem.Worker**: Background worker service for event consumption
  - Consumers: `OrderCreatedConsumer`, `OrderCompletedConsumer`, `OrderExpiredConsumer`
- **AI.OrderProcessingSystem.CronJob**: Scheduled task service for order expiry
  - Services: `OrderExpiryService`
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
# Rebuild all containers
docker-compose down -v
docker-compose up --build
```

**View service logs**:
```bash
# View all logs
docker-compose logs -f

# View specific service logs
docker logs orderprocessing-api
docker logs orderprocessing-worker
docker logs orderprocessing-cronjob
docker logs orderprocessing-rabbitmq
```

**Check order processing**:
```bash
# Check orders in database
docker exec orderprocessing-db psql -U postgres -d orderprocessing -c "SELECT id, status, created_at, updated_at FROM orders ORDER BY id DESC LIMIT 10;"

# Check notifications
docker exec orderprocessing-db psql -U postgres -d orderprocessing -c "SELECT id, order_id, event_type, is_email_sent FROM notifications ORDER BY id DESC LIMIT 10;"
```

**Port conflicts**:
Edit `docker-compose.yml` to change port mappings if 5432, 5115, or 7037 are already in use.

## License

Proprietary - All rights reserved
