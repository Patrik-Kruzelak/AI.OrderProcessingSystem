## FEATURE:

Adding Event-Driven Architecture + Background Processing

We will need to create a new table Notifications with the following columns: id, order_id, eventType (enum: OrderCreated, OrderCompleted, OrderExpired), message (string), is_email_sent (bool), created_at. We do not need CRUD endpoints for this table.

All asynchronous operations, including handling events from the event bus, should be implemented in the AI.OrderProcessingSystem.Worker project.

I want to use RabbitMQ and an event bus, which needs to be run in the existing Dockerfile. I also want to run AI.OrderProcessingSystem.Worker and AI.OrderProcessingSystem.CronJob there.

Order Creation Flow:
1. User creates an order via POST /api/orders. (WebApi)
2. Order is saved in the DB with status=pending.
3. OrderCreated event is published. (From WebApi)
4. Worker handles the OrderCreated event asynchronously: (Worker)
- Update order status: pending → processing.
- Simulate payment processing (5 second delay).
- For 50% of cases, update status → completed and publish OrderCompleted.
- For 50% of cases, leave status as processing.

Notifications: (this will be done in consumers in the Worker)
- Create a Notifications table in the database.
- When OrderCompleted is published:
--- Log a fake email to the console.
--- Save a notification in the DB (audit trail).
- When OrderExpired is published:
--- Save a notification in the DB (audit trail).

I will also need to run a cron job, which will be implemented in AI.OrderProcessingSystem.CronJob, and should work as follows:
- CronJob runs every 60 seconds.
- Finds orders with status=processing older than 10 minutes.
- Updates status → expired.
- Publishes OrderExpired event.

All constants, such as 50%, 10 minutes, 60 seconds, etc., should be added to \Configuration\instance.json.

If necessary, update the README.md file again.

When implementing, try to use best practices.

If necessary, adjust the type of existing projects that are currently created as console applications. The console applications were generated only as an initial skeleton.

## EXAMPLES:

### Existing Entity Pattern
Look at the **Order entity** in `AI.OrderProcessingSystem.Dal\Entities\Order.cs:1-40` to understand the entity pattern used in this codebase:
- Uses data annotations: `[Table]`, `[Column]`, `[Required]`, `[MaxLength]`
- Snake_case column names (e.g., `created_at`, `user_id`)
- Navigation properties for relationships
- Status stored as string with MaxLength(20)

The Notification entity should follow this same pattern.

### Existing DbContext Configuration
Look at **OrderProcessingDbContext** in `AI.OrderProcessingSystem.Dal\Data\OrderProcessingDbContext.cs:1-91`:
- DbSets are defined for each entity (line 13-16)
- `OnModelCreating` configures entities with Fluent API (line 18-89)
- Check constraints are used for validation (e.g., line 60-61 for order status)
- Foreign key relationships are configured explicitly (e.g., line 52-56)

Add the Notifications DbSet and configure it in `OnModelCreating` following this pattern.

### Existing Order Controller
The **OrdersController** in `AI.OrderProcessingSystem.WebApi\Controllers\OrdersController.cs:1-200` shows:
- How to inject `OrderProcessingDbContext` (line 15, 18-22)
- How to create entities and save them (line 92-103)
- Current order creation flow in the `Create` method (line 58-116)
- Status is set to "pending" on line 96

You'll need to publish an OrderCreated event after line 103 (`_context.Orders.Add(order)`).

### Existing Configuration Pattern
The **Program.cs** in `AI.OrderProcessingSystem.WebApi\Program.cs:14-46` demonstrates:
- How to load JSON configuration from `\Configuration\secrets.json` and `\Configuration\instance.json` (line 15-26)
- How to deserialize into strongly-typed configuration objects (line 29-36)
- How to validate configuration (line 38-42)
- How to register configuration in DI (line 45-46)

See **AppConfiguration** structure in `AI.OrderProcessingSystem.WebApi\Configuration\AppConfiguration.cs:1-23` for how configuration classes are organized.

Add new event processing settings (like 50% completion rate, 10-minute expiry threshold, 60-second cron interval) to `instance.json` and create corresponding configuration classes.

### Existing Docker Setup
The **docker-compose.yml** at root (`docker-compose.yml:1-49`) shows:
- PostgreSQL service configuration (line 4-21)
- WebApi service that depends on postgres with health check (line 23-42)
- Volume mounting for Configuration folder (line 41)
- Network setup (line 46-48)

You need to add:
- RabbitMQ service (similar to postgres service)
- Worker service (similar to webapi service)
- CronJob service (similar to webapi service)

The **Dockerfile** at root (`Dockerfile:1-41`) shows how the WebApi is built in multi-stage build.

### Existing Worker and CronJob Projects
Both `AI.OrderProcessingSystem.Worker\Program.cs` and `AI.OrderProcessingSystem.CronJob\Program.cs` currently just output "Hello, World!" (see CLAUDE.md:94-104).

You'll need to:
- Change Worker from console app to hosted service (.NET Generic Host or Worker Service)
- Change CronJob from console app to hosted service with timer/scheduler
- Both will need to reference Dal and Common projects
- Both will need configuration loading similar to WebApi

### Enum Pattern
An **OrderStatus enum** already exists in `AI.OrderProcessingSystem.Common\Enums\OrderStatus.cs:1-10` but it's NOT currently used in the Order entity.

The Order entity uses string status values validated by check constraint (see `OrderProcessingDbContext.cs:60-61`).

For the Notification entity's `eventType`, you should create a similar enum in the Common project and store it as a string with a check constraint.

## DOCUMENTATION:

### RabbitMQ Integration
- **RabbitMQ .NET Client**: https://www.rabbitmq.com/client-libraries/dotnet-api-guide
- **MassTransit with RabbitMQ**: https://masstransit.io/documentation/transports/rabbitmq (if using MassTransit)
- **RabbitMQ Docker Image**: https://hub.docker.com/_/rabbitmq

### .NET Background Services
- **.NET Worker Services**: https://learn.microsoft.com/en-us/dotnet/core/extensions/workers
- **.NET Generic Host**: https://learn.microsoft.com/en-us/dotnet/core/extensions/generic-host
- **Background tasks with hosted services**: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services

### Entity Framework Core
- **EF Core Migrations**: https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/
- **DbContext Configuration**: https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/

### Timer/Scheduling for CronJob
- **PeriodicTimer (.NET 6+)**: https://learn.microsoft.com/en-us/dotnet/api/system.threading.periodictimer
- **NCrontab** (if more complex scheduling needed): https://github.com/atifaziz/NCrontab

## OTHER CONSIDERATIONS:

### Configuration File Location
The Configuration folder (containing secrets.json and instance.json) is expected at the **repository root** level (see `Program.cs:15-16` where it uses `Path.Combine(Directory.GetCurrentDirectory(), "..", "Configuration", ...)`).

Currently, this folder does NOT exist in the repository (ls output shows no Configuration folder). You'll need to create:
- `\Configuration\secrets.json` - add RabbitMQ credentials if needed
- `\Configuration\instance.json` - add event processing settings (50% rate, timeouts, intervals)

Both files should be mounted into Docker containers (see docker-compose.yml:41).

### Project References
Per CLAUDE.md architecture rules:
- **WebApi** may reference Dal and Common, but NOT Worker or CronJob
- **Worker** may reference Dal and Common, but NOT WebApi or CronJob
- **CronJob** may reference Dal and Common, but NOT WebApi or Worker
- **Common** must NOT reference any other project (it's the shared foundation)
- **Dal** may be referenced by all projects except Common

This means:
- Event publishing from WebApi → Worker must go through RabbitMQ (cannot directly reference Worker)
- Shared event contracts (DTOs) should be in Common project
- Database entities and DbContext are in Dal and can be used by Worker and CronJob

### Database Migration
The WebApi currently runs migrations on startup (see `Program.cs:145-150`). When adding the Notifications table:
- Create a new migration in the Dal project
- The migration will run automatically when WebApi starts
- Worker and CronJob should NOT run migrations, only WebApi should

### Order Status Constraint
The Order entity has a check constraint for valid statuses (`OrderProcessingDbContext.cs:60-61`):
```csharp
"status IN ('pending', 'processing', 'completed', 'expired')"
```

This constraint already supports all four statuses you need. Ensure status updates in Worker and CronJob use these exact lowercase string values.

### Console Application → Worker Service Conversion
Both Worker and CronJob projects are currently `.csproj` with `OutputType: Exe` and SDK: Microsoft.NET.Sdk (see CLAUDE.md:81-104).

To convert to proper worker services:
1. Change SDK to `Microsoft.NET.Sdk.Worker` if appropriate, or keep as `Microsoft.NET.Sdk`
2. Add NuGet packages: `Microsoft.Extensions.Hosting`, `Microsoft.Extensions.DependencyInjection`
3. Replace "Hello World" Program.cs with Generic Host builder pattern
4. Implement `BackgroundService` or `IHostedService`

### Docker Healthchecks
The postgres service has a healthcheck (docker-compose.yml:15-19) and webapi depends on it (line 28-30). Consider adding:
- Healthcheck for RabbitMQ service
- Worker and CronJob services should depend on both postgres and rabbitmq being healthy

### Gotchas
1. **Time zones**: Order.CreatedAt and UpdatedAt use DateTime.UtcNow (see `OrdersController.cs:98-99`). Use UTC consistently for timestamp comparisons in CronJob.
2. **Race conditions**: If multiple CronJob instances run, use database-level locking or unique constraints to prevent duplicate processing of the same order.
3. **Event ordering**: RabbitMQ doesn't guarantee order across queues. If order matters, use a single queue or message ordering features.
4. **Connection resilience**: RabbitMQ connections can drop. Implement retry logic and connection recovery in Worker and WebApi.
5. **Testing**: The existing test project (`AI.OrderProcessingSystem.WebApi.Tests`) uses Testcontainers. You may want to add RabbitMQ test containers for integration testing event flows.
