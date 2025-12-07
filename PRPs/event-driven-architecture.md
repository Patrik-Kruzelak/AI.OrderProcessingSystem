# PRP: Event-Driven Architecture with RabbitMQ

## Feature Summary
Implement event-driven architecture using RabbitMQ to enable asynchronous order processing. Orders created via the WebApi will publish events consumed by a Worker service for payment simulation. A CronJob service will expire old processing orders. All events will trigger notification records for audit trails.

**Confidence Score: 8/10**
- High confidence due to comprehensive context and established patterns
- Complexity in Docker orchestration and multi-service coordination reduces by 2 points
- Clear validation gates and existing test infrastructure supports one-pass implementation

---

## Technology Stack Decision

### Recommended: MassTransit
Despite the user expressing interest in "trying something new", **MassTransit remains the recommended approach** for the following technical reasons:

**Advantages:**
- Industry-standard abstraction for .NET messaging (used by thousands of production systems)
- Built-in patterns: retry policies, circuit breakers, error handling, dead letter queues
- Transport abstraction (can swap RabbitMQ for Azure Service Bus or Amazon SQS without code changes)
- Message serialization with versioning support
- Saga state machine support for complex workflows
- Excellent .NET integration with dependency injection and logging
- Active maintenance and .NET 8/9 support

**Documentation:**
- [MassTransit with RabbitMQ - Milan Jovanovic](https://www.milanjovanovic.tech/blog/using-masstransit-with-rabbitmq-and-azure-service-bus)
- [RabbitMQ Configuration · MassTransit](https://masstransit.io/documentation/configuration/transports/rabbitmq)
- [MassTransit Quick Start](https://masstransit.io/quick-starts/rabbitmq)
- [Using MassTransit in ASP.NET Core - Code Maze](https://code-maze.com/masstransit-rabbitmq-aspnetcore/)

### Alternative: RabbitMQ.Client (Raw Client)
If truly avoiding MassTransit, the raw RabbitMQ.Client can be used, but requires manual implementation of:
- Connection management and pooling
- Message serialization/deserialization
- Retry logic and error handling
- Dead letter queue configuration
- Message acknowledgment patterns

This PRP will focus on **MassTransit** for optimal one-pass implementation success.

---

## Prerequisites & Context

### Existing Codebase Patterns

**Entity Pattern** (`AI.OrderProcessingSystem.Dal\Entities\Order.cs:1-40`):
- Data annotations: `[Table("table_name")]`, `[Column("column_name")]`
- Snake_case column names
- Navigation properties for relationships
- String-based status with check constraints

**Configuration Pattern** (`AI.OrderProcessingSystem.WebApi\Program.cs:14-46`):
- Load JSON files from `\Configuration\` folder
- Deserialize to strongly-typed classes
- Validate at startup
- Register as singletons in DI

**Service Pattern** (`AI.OrderProcessingSystem.WebApi\Services\JwtTokenService.cs`):
- Interface + implementation (e.g., `IJwtTokenService` / `JwtTokenService`)
- Registered as scoped services in DI
- Constructor injection of dependencies

**Testing Pattern** (`AI.OrderProcessingSystem.WebApi.Tests\Fixtures\CustomWebApplicationFactory.cs`):
- xUnit test framework
- WebApplicationFactory<Program> with custom configuration
- Testcontainers for PostgreSQL
- IAsyncLifetime for test setup/teardown

### Current Project State

**Worker and CronJob Projects:**
- Both are minimal console apps (OutputType: Exe, Sdk: Microsoft.NET.Sdk)
- Currently output "Hello, World!" (`AI.OrderProcessingSystem.Worker\Program.cs:2`)
- No project references or NuGet packages
- Need conversion to Worker Services

**Configuration Folder:**
- Does NOT currently exist in repository (checked via `ls` output)
- Expected at repository root: `\Configuration\`
- Must contain `secrets.json` and `instance.json`

**Existing Order Status Values:**
- Database constraint: `"status IN ('pending', 'processing', 'completed', 'expired')"` (`AI.OrderProcessingSystem.Dal\Data\OrderProcessingDbContext.cs:60-61`)
- All four required statuses already supported

---

## Implementation Blueprint

### Phase 1: Configuration Foundation

#### 1.1 Create Configuration Files

**Create `\Configuration\instance.json`:**
```json
{
  "AppSettings": {
    "ApiTitle": "AI Order Processing System",
    "ApiVersion": "v1.0",
    "ApiDescription": "Event-driven order processing system with RabbitMQ"
  },
  "EventProcessingSettings": {
    "PaymentProcessingDelaySeconds": 5,
    "OrderCompletionSuccessRate": 0.5,
    "OrderExpiryThresholdMinutes": 10,
    "ExpiryCheckIntervalSeconds": 60
  },
  "RabbitMqSettings": {
    "Host": "localhost",
    "VirtualHost": "/",
    "Port": 5672
  }
}
```

**Create `\Configuration\secrets.json`:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=orderprocessing;Username=postgres;Password=SecureP@ssw0rd123"
  },
  "JwtSettings": {
    "SecretKey": "your-secret-key-must-be-at-least-32-characters-long-for-hs256",
    "Issuer": "AI.OrderProcessingSystem",
    "Audience": "AI.OrderProcessingSystem.Users",
    "ExpirationMinutes": 120
  },
  "AdminUser": {
    "Email": "admin@orderprocessing.local",
    "Password": "Admin@12345"
  },
  "RabbitMqSettings": {
    "Username": "guest",
    "Password": "guest"
  }
}
```

**Note:** These files should be created at the repository root level. The WebApi expects to find them at `Path.Combine(Directory.GetCurrentDirectory(), "..", "Configuration", ...)`.

#### 1.2 Create Configuration Classes

**Create `AI.OrderProcessingSystem.Common\Configuration\EventProcessingSettings.cs`:**
```csharp
namespace AI.OrderProcessingSystem.Common.Configuration;

public class EventProcessingSettings
{
    public int PaymentProcessingDelaySeconds { get; set; }
    public double OrderCompletionSuccessRate { get; set; }
    public int OrderExpiryThresholdMinutes { get; set; }
    public int ExpiryCheckIntervalSeconds { get; set; }
}
```

**Create `AI.OrderProcessingSystem.Common\Configuration\RabbitMqSettings.cs`:**
```csharp
namespace AI.OrderProcessingSystem.Common.Configuration;

public class RabbitMqSettings
{
    public string Host { get; set; } = string.Empty;
    public string VirtualHost { get; set; } = "/";
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
```

**Update `AI.OrderProcessingSystem.WebApi\Configuration\AppConfiguration.cs`:**
Add these properties:
```csharp
public EventProcessingSettings EventProcessingSettings { get; set; } = new();
public RabbitMqSettings RabbitMqSettings { get; set; } = new();
```

**Update `AI.OrderProcessingSystem.WebApi\Program.cs`** configuration loading section (after line 36):
```csharp
EventProcessingSettings = JsonSerializer.Deserialize<EventProcessingSettings>(
    instanceConfig["EventProcessingSettings"].GetRawText())!,
RabbitMqSettings = new RabbitMqSettings
{
    Host = instanceConfig["RabbitMqSettings"].GetProperty("Host").GetString()!,
    VirtualHost = instanceConfig["RabbitMqSettings"].GetProperty("VirtualHost").GetString()!,
    Port = instanceConfig["RabbitMqSettings"].GetProperty("Port").GetInt32(),
    Username = secretsConfig["RabbitMqSettings"].GetProperty("Username").GetString()!,
    Password = secretsConfig["RabbitMqSettings"].GetProperty("Password").GetString()!
}
```

Add validation (after line 42):
```csharp
if (string.IsNullOrEmpty(appConfig.RabbitMqSettings.Host))
    throw new InvalidOperationException("RabbitMQ host is not configured");
if (appConfig.EventProcessingSettings.OrderCompletionSuccessRate < 0 ||
    appConfig.EventProcessingSettings.OrderCompletionSuccessRate > 1)
    throw new InvalidOperationException("OrderCompletionSuccessRate must be between 0 and 1");
```

Register configuration (after line 46):
```csharp
builder.Services.AddSingleton(appConfig.EventProcessingSettings);
builder.Services.AddSingleton(appConfig.RabbitMqSettings);
```

---

### Phase 2: Event Contracts

#### 2.1 Create Event DTOs in Common Project

**Create `AI.OrderProcessingSystem.Common\Events\OrderCreatedEvent.cs`:**
```csharp
namespace AI.OrderProcessingSystem.Common.Events;

public record OrderCreatedEvent
{
    public int OrderId { get; init; }
    public int UserId { get; init; }
    public decimal Total { get; init; }
    public DateTime CreatedAt { get; init; }
}
```

**Create `AI.OrderProcessingSystem.Common\Events\OrderCompletedEvent.cs`:**
```csharp
namespace AI.OrderProcessingSystem.Common.Events;

public record OrderCompletedEvent
{
    public int OrderId { get; init; }
    public int UserId { get; init; }
    public decimal Total { get; init; }
    public DateTime CompletedAt { get; init; }
}
```

**Create `AI.OrderProcessingSystem.Common\Events\OrderExpiredEvent.cs`:**
```csharp
namespace AI.OrderProcessingSystem.Common.Events;

public record OrderExpiredEvent
{
    public int OrderId { get; init; }
    public int UserId { get; init; }
    public DateTime ExpiredAt { get; init; }
    public DateTime OriginalCreatedAt { get; init; }
}
```

**Why records?** Records provide value-based equality, immutability, and concise syntax - ideal for event DTOs that should not change after creation.

#### 2.2 Create Event Publisher Abstraction

**Create `AI.OrderProcessingSystem.Common\Abstractions\IEventPublisher.cs`:**
```csharp
namespace AI.OrderProcessingSystem.Common.Abstractions;

public interface IEventPublisher
{
    Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class;
}
```

This abstraction decouples the rest of the codebase from MassTransit, making it easier to swap implementations if needed.

---

### Phase 3: Database Schema

#### 3.1 Create Notification Entity

**Create `AI.OrderProcessingSystem.Dal\Entities\Notification.cs`:**
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AI.OrderProcessingSystem.Dal.Entities;

[Table("notifications")]
public class Notification
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("order_id")]
    public int OrderId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("event_type")]
    public string EventType { get; set; } = string.Empty;

    [Required]
    [Column("message")]
    public string Message { get; set; } = string.Empty;

    [Required]
    [Column("is_email_sent")]
    public bool IsEmailSent { get; set; }

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    // Navigation property
    [ForeignKey(nameof(OrderId))]
    public Order Order { get; set; } = null!;
}
```

**Pattern Reference:** Follows the same pattern as `AI.OrderProcessingSystem.Dal\Entities\Order.cs:1-40`.

#### 3.2 Create EventType Enum

**Create `AI.OrderProcessingSystem.Common\Enums\EventType.cs`:**
```csharp
namespace AI.OrderProcessingSystem.Common.Enums;

public enum EventType
{
    OrderCreated,
    OrderCompleted,
    OrderExpired
}
```

**Note:** Similar to existing `OrderStatus` enum in `AI.OrderProcessingSystem.Common\Enums\OrderStatus.cs:1-10`. Store as string in database with check constraint.

#### 3.3 Configure DbContext

**Update `AI.OrderProcessingSystem.Dal\Data\OrderProcessingDbContext.cs`:**

Add DbSet (after line 16):
```csharp
public DbSet<Notification> Notifications { get; set; }
```

Add configuration in `OnModelCreating` (after line 84):
```csharp
// Notification configuration
modelBuilder.Entity<Notification>(entity =>
{
    entity.Property(e => e.EventType).IsRequired().HasMaxLength(50);
    entity.Property(e => e.Message).IsRequired();
    entity.Property(e => e.IsEmailSent).IsRequired();
    entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

    // Foreign key
    entity.HasOne(e => e.Order)
        .WithMany()
        .HasForeignKey(e => e.OrderId)
        .OnDelete(DeleteBehavior.Cascade);

    // Check constraint for event type
    entity.HasCheckConstraint("CK_Notification_EventType",
        "event_type IN ('OrderCreated', 'OrderCompleted', 'OrderExpired')");

    // Index for querying by order
    entity.HasIndex(e => e.OrderId);
});
```

**Pattern Reference:** Follows `OrderProcessingDbContext.cs:44-62` pattern with check constraints and foreign keys.

#### 3.4 Create Migration

**Run from Dal project directory:**
```bash
cd AI.OrderProcessingSystem.Dal
dotnet ef migrations add AddNotificationsTable --startup-project ../AI.OrderProcessingSystem.WebApi
```

**Validation:** Migration file should be created in `AI.OrderProcessingSystem.Dal\Migrations\` with `Up()` and `Down()` methods.

**Note:** Migration will run automatically when WebApi starts (see `Program.cs:145-150`).

---

### Phase 4: MassTransit Infrastructure

#### 4.1 Add NuGet Packages

**WebApi project:**
```bash
cd AI.OrderProcessingSystem.WebApi
dotnet add package MassTransit
dotnet add package MassTransit.RabbitMQ
```

**Worker project:**
```bash
cd AI.OrderProcessingSystem.Worker
dotnet add package MassTransit
dotnet add package MassTransit.RabbitMQ
dotnet add package Microsoft.Extensions.Hosting
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
```

**CronJob project:**
```bash
cd AI.OrderProcessingSystem.CronJob
dotnet add package MassTransit
dotnet add package MassTransit.RabbitMQ
dotnet add package Microsoft.Extensions.Hosting
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
```

**Common project** (for IEventPublisher):
No MassTransit package needed in Common - keep it clean of transport dependencies.

#### 4.2 Add Project References

**Worker project:**
```bash
cd AI.OrderProcessingSystem.Worker
dotnet add reference ../AI.OrderProcessingSystem.Common/AI.OrderProcessingSystem.Common.csproj
dotnet add reference ../AI.OrderProcessingSystem.Dal/AI.OrderProcessingSystem.Dal.csproj
```

**CronJob project:**
```bash
cd AI.OrderProcessingSystem.CronJob
dotnet add reference ../AI.OrderProcessingSystem.Common/AI.OrderProcessingSystem.Common.csproj
dotnet add reference ../AI.OrderProcessingSystem.Dal/AI.OrderProcessingSystem.Dal.csproj
```

**Validation:** Check `.csproj` files contain `<ProjectReference>` elements.

#### 4.3 Create MassTransit Event Publisher Implementation

**Create `AI.OrderProcessingSystem.WebApi\Services\MassTransitEventPublisher.cs`:**
```csharp
using AI.OrderProcessingSystem.Common.Abstractions;
using MassTransit;

namespace AI.OrderProcessingSystem.WebApi.Services;

public class MassTransitEventPublisher : IEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<MassTransitEventPublisher> _logger;

    public MassTransitEventPublisher(
        IPublishEndpoint publishEndpoint,
        ILogger<MassTransitEventPublisher> logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : class
    {
        try
        {
            await _publishEndpoint.Publish(message, cancellationToken);
            _logger.LogInformation("Published event: {EventType}", typeof(T).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event: {EventType}", typeof(T).Name);
            throw;
        }
    }
}
```

**Pattern Reference:** Follows the interface + implementation pattern from `JwtTokenService.cs:10-47`.

#### 4.4 Configure MassTransit in WebApi

**Update `AI.OrderProcessingSystem.WebApi\Program.cs`** (after line 54):
```csharp
// Add MassTransit
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(appConfig.RabbitMqSettings.Host, appConfig.RabbitMqSettings.VirtualHost, h =>
        {
            h.Username(appConfig.RabbitMqSettings.Username);
            h.Password(appConfig.RabbitMqSettings.Password);
        });

        cfg.ConfigureEndpoints(context);
    });
});

// Register event publisher
builder.Services.AddScoped<IEventPublisher, MassTransitEventPublisher>();
```

**Documentation:**
- [MassTransit RabbitMQ Configuration](https://masstransit.io/documentation/configuration/transports/rabbitmq)
- [Worker Services - .NET Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/extensions/workers)

---

### Phase 5: WebApi Event Publishing

#### 5.1 Update OrdersController

**Modify `AI.OrderProcessingSystem.WebApi\Controllers\OrdersController.cs`:**

Add IEventPublisher dependency (update constructor around line 18-22):
```csharp
private readonly OrderProcessingDbContext _context;
private readonly ILogger<OrdersController> _logger;
private readonly IEventPublisher _eventPublisher;

public OrdersController(
    OrderProcessingDbContext context,
    ILogger<OrdersController> logger,
    IEventPublisher eventPublisher)
{
    _context = context;
    _logger = logger;
    _eventPublisher = eventPublisher;
}
```

Add event publishing after order creation (after line 103 `_context.SaveChangesAsync()`):
```csharp
await _context.SaveChangesAsync();

// Publish OrderCreated event
var orderCreatedEvent = new OrderCreatedEvent
{
    OrderId = order.Id,
    UserId = order.UserId,
    Total = order.Total,
    CreatedAt = order.CreatedAt
};

await _eventPublisher.PublishAsync(orderCreatedEvent);

_logger.LogInformation("Order created with ID {OrderId}", order.Id);
```

Add using statement at top:
```csharp
using AI.OrderProcessingSystem.Common.Events;
using AI.OrderProcessingSystem.Common.Abstractions;
```

**Critical Consideration:** Event is published AFTER `SaveChangesAsync()` succeeds. If publishing fails, the order exists in DB but no event is sent. This is acceptable for this implementation, but in production, consider:
- Transactional Outbox pattern (MassTransit supports this via `EntityFrameworkOutboxConfigurer`)
- Retry logic with exponential backoff
- Dead letter queue for failed publishes

---

### Phase 6: Worker Service Implementation

#### 6.1 Convert Worker Project to Worker Service

**Replace `AI.OrderProcessingSystem.Worker\Program.cs`:**
```csharp
using AI.OrderProcessingSystem.Common.Configuration;
using AI.OrderProcessingSystem.Dal.Data;
using AI.OrderProcessingSystem.Worker.Consumers;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

var builder = Host.CreateApplicationBuilder(args);

// Load configuration files
var secretsPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Configuration", "secrets.json");
var instancePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Configuration", "instance.json");

if (!File.Exists(secretsPath))
    throw new FileNotFoundException($"Configuration file not found: {secretsPath}");
if (!File.Exists(instancePath))
    throw new FileNotFoundException($"Configuration file not found: {instancePath}");

var secretsConfig = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
    File.ReadAllText(secretsPath), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
var instanceConfig = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
    File.ReadAllText(instancePath), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

// Parse configuration
var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                       ?? secretsConfig!["ConnectionStrings"].GetProperty("DefaultConnection").GetString()!;

var rabbitMqSettings = new RabbitMqSettings
{
    Host = instanceConfig!["RabbitMqSettings"].GetProperty("Host").GetString()!,
    VirtualHost = instanceConfig["RabbitMqSettings"].GetProperty("VirtualHost").GetString()!,
    Port = instanceConfig["RabbitMqSettings"].GetProperty("Port").GetInt32(),
    Username = secretsConfig["RabbitMqSettings"].GetProperty("Username").GetString()!,
    Password = secretsConfig["RabbitMqSettings"].GetProperty("Password").GetString()!
};

var eventProcessingSettings = JsonSerializer.Deserialize<EventProcessingSettings>(
    instanceConfig["EventProcessingSettings"].GetRawText())!;

// Register configuration
builder.Services.AddSingleton(eventProcessingSettings);
builder.Services.AddSingleton(rabbitMqSettings);

// Add DbContext
builder.Services.AddDbContext<OrderProcessingDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add MassTransit with RabbitMQ
builder.Services.AddMassTransit(x =>
{
    // Register consumers
    x.AddConsumer<OrderCreatedConsumer>();
    x.AddConsumer<OrderCompletedConsumer>();
    x.AddConsumer<OrderExpiredConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitMqSettings.Host, rabbitMqSettings.VirtualHost, h =>
        {
            h.Username(rabbitMqSettings.Username);
            h.Password(rabbitMqSettings.Password);
        });

        cfg.ConfigureEndpoints(context);
    });
});

var host = builder.Build();
host.Run();
```

**Pattern Reference:** Configuration loading follows `WebApi\Program.cs:14-46`. Worker Service pattern from [.NET Worker Services Documentation](https://learn.microsoft.com/en-us/dotnet/core/extensions/workers).

#### 6.2 Create OrderCreatedConsumer

**Create `AI.OrderProcessingSystem.Worker\Consumers\OrderCreatedConsumer.cs`:**
```csharp
using AI.OrderProcessingSystem.Common.Abstractions;
using AI.OrderProcessingSystem.Common.Configuration;
using AI.OrderProcessingSystem.Common.Events;
using AI.OrderProcessingSystem.Dal.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AI.OrderProcessingSystem.Worker.Consumers;

public class OrderCreatedConsumer : IConsumer<OrderCreatedEvent>
{
    private readonly OrderProcessingDbContext _context;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly EventProcessingSettings _settings;
    private readonly ILogger<OrderCreatedConsumer> _logger;

    public OrderCreatedConsumer(
        OrderProcessingDbContext context,
        IPublishEndpoint publishEndpoint,
        EventProcessingSettings settings,
        ILogger<OrderCreatedConsumer> logger)
    {
        _context = context;
        _publishEndpoint = publishEndpoint;
        _settings = settings;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Processing OrderCreated event for Order {OrderId}", message.OrderId);

        try
        {
            // Find the order
            var order = await _context.Orders.FindAsync(message.OrderId);
            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found", message.OrderId);
                return;
            }

            // Update status to processing
            order.Status = "processing";
            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Order {OrderId} status updated to processing", message.OrderId);

            // Simulate payment processing
            await Task.Delay(TimeSpan.FromSeconds(_settings.PaymentProcessingDelaySeconds));

            // Determine if payment succeeds (based on configured success rate)
            var random = new Random();
            var isSuccessful = random.NextDouble() < _settings.OrderCompletionSuccessRate;

            if (isSuccessful)
            {
                // Update status to completed
                order.Status = "completed";
                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Publish OrderCompleted event
                var completedEvent = new OrderCompletedEvent
                {
                    OrderId = order.Id,
                    UserId = order.UserId,
                    Total = order.Total,
                    CompletedAt = order.UpdatedAt
                };

                await _publishEndpoint.Publish(completedEvent);

                _logger.LogInformation("Order {OrderId} completed successfully", message.OrderId);
            }
            else
            {
                _logger.LogInformation("Order {OrderId} remains in processing state", message.OrderId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing OrderCreated event for Order {OrderId}", message.OrderId);
            throw; // Let MassTransit handle retry
        }
    }
}
```

**Critical Implementation Notes:**
- Uses `Random` for 50% success rate - in production, use `Random.Shared` (.NET 6+) or inject `IRandomGenerator`
- Status updates follow the flow: pending → processing → completed (or stay processing)
- MassTransit will automatically retry on exception based on default retry policy
- Delay is configurable via `EventProcessingSettings.PaymentProcessingDelaySeconds`

#### 6.3 Create OrderCompletedConsumer

**Create `AI.OrderProcessingSystem.Worker\Consumers\OrderCompletedConsumer.cs`:**
```csharp
using AI.OrderProcessingSystem.Common.Events;
using AI.OrderProcessingSystem.Dal.Data;
using AI.OrderProcessingSystem.Dal.Entities;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace AI.OrderProcessingSystem.Worker.Consumers;

public class OrderCompletedConsumer : IConsumer<OrderCompletedEvent>
{
    private readonly OrderProcessingDbContext _context;
    private readonly ILogger<OrderCompletedConsumer> _logger;

    public OrderCompletedConsumer(
        OrderProcessingDbContext context,
        ILogger<OrderCompletedConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCompletedEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Processing OrderCompleted event for Order {OrderId}", message.OrderId);

        try
        {
            // Log fake email to console
            _logger.LogInformation(
                "📧 SENDING EMAIL: Order #{OrderId} completed! Total: ${Total:F2}. Thank you for your order!",
                message.OrderId,
                message.Total);

            // Save notification to database (audit trail)
            var notification = new Notification
            {
                OrderId = message.OrderId,
                EventType = "OrderCompleted",
                Message = $"Order #{message.OrderId} completed successfully. Total: ${message.Total:F2}",
                IsEmailSent = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Notification saved for Order {OrderId}", message.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing OrderCompleted event for Order {OrderId}", message.OrderId);
            throw;
        }
    }
}
```

**Note:** Email is simulated with a log message (📧 emoji for visibility in console output). In production, integrate with email service (SendGrid, AWS SES, etc.).

#### 6.4 Create OrderExpiredConsumer

**Create `AI.OrderProcessingSystem.Worker\Consumers\OrderExpiredConsumer.cs`:**
```csharp
using AI.OrderProcessingSystem.Common.Events;
using AI.OrderProcessingSystem.Dal.Data;
using AI.OrderProcessingSystem.Dal.Entities;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace AI.OrderProcessingSystem.Worker.Consumers;

public class OrderExpiredConsumer : IConsumer<OrderExpiredEvent>
{
    private readonly OrderProcessingDbContext _context;
    private readonly ILogger<OrderExpiredConsumer> _logger;

    public OrderExpiredConsumer(
        OrderProcessingDbContext context,
        ILogger<OrderExpiredConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderExpiredEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Processing OrderExpired event for Order {OrderId}", message.OrderId);

        try
        {
            // Save notification to database (audit trail)
            var notification = new Notification
            {
                OrderId = message.OrderId,
                EventType = "OrderExpired",
                Message = $"Order #{message.OrderId} expired after being in processing state for too long.",
                IsEmailSent = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Expiry notification saved for Order {OrderId}", message.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing OrderExpired event for Order {OrderId}", message.OrderId);
            throw;
        }
    }
}
```

---

### Phase 7: CronJob Service Implementation

#### 7.1 Convert CronJob Project to Worker Service

**Replace `AI.OrderProcessingSystem.CronJob\Program.cs`:**
```csharp
using AI.OrderProcessingSystem.Common.Configuration;
using AI.OrderProcessingSystem.CronJob.Services;
using AI.OrderProcessingSystem.Dal.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

var builder = Host.CreateApplicationBuilder(args);

// Load configuration files
var secretsPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Configuration", "secrets.json");
var instancePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Configuration", "instance.json");

if (!File.Exists(secretsPath))
    throw new FileNotFoundException($"Configuration file not found: {secretsPath}");
if (!File.Exists(instancePath))
    throw new FileNotFoundException($"Configuration file not found: {instancePath}");

var secretsConfig = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
    File.ReadAllText(secretsPath), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
var instanceConfig = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
    File.ReadAllText(instancePath), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

// Parse configuration
var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                       ?? secretsConfig!["ConnectionStrings"].GetProperty("DefaultConnection").GetString()!;

var rabbitMqSettings = new RabbitMqSettings
{
    Host = instanceConfig!["RabbitMqSettings"].GetProperty("Host").GetString()!,
    VirtualHost = instanceConfig["RabbitMqSettings"].GetProperty("VirtualHost").GetString()!,
    Port = instanceConfig["RabbitMqSettings"].GetProperty("Port").GetInt32(),
    Username = secretsConfig["RabbitMqSettings"].GetProperty("Username").GetString()!,
    Password = secretsConfig["RabbitMqSettings"].GetProperty("Password").GetString()!
};

var eventProcessingSettings = JsonSerializer.Deserialize<EventProcessingSettings>(
    instanceConfig["EventProcessingSettings"].GetRawText())!;

// Register configuration
builder.Services.AddSingleton(eventProcessingSettings);
builder.Services.AddSingleton(rabbitMqSettings);

// Add DbContext with scoped lifetime
builder.Services.AddDbContext<OrderProcessingDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add MassTransit for publishing only (no consumers)
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitMqSettings.Host, rabbitMqSettings.VirtualHost, h =>
        {
            h.Username(rabbitMqSettings.Username);
            h.Password(rabbitMqSettings.Password);
        });
    });
});

// Register the background service
builder.Services.AddHostedService<OrderExpiryService>();

var host = builder.Build();
host.Run();
```

#### 7.2 Create OrderExpiryService

**Create `AI.OrderProcessingSystem.CronJob\Services\OrderExpiryService.cs`:**
```csharp
using AI.OrderProcessingSystem.Common.Configuration;
using AI.OrderProcessingSystem.Common.Events;
using AI.OrderProcessingSystem.Dal.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AI.OrderProcessingSystem.CronJob.Services;

public class OrderExpiryService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly EventProcessingSettings _settings;
    private readonly ILogger<OrderExpiryService> _logger;
    private readonly PeriodicTimer _timer;

    public OrderExpiryService(
        IServiceProvider serviceProvider,
        EventProcessingSettings settings,
        ILogger<OrderExpiryService> logger)
    {
        _serviceProvider = serviceProvider;
        _settings = settings;
        _logger = logger;
        _timer = new PeriodicTimer(TimeSpan.FromSeconds(settings.ExpiryCheckIntervalSeconds));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OrderExpiryService started. Checking every {Interval} seconds",
            _settings.ExpiryCheckIntervalSeconds);

        // Wait for initial delay to allow other services to start
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (await _timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
        {
            await CheckAndExpireOrders(stoppingToken);
        }
    }

    private async Task CheckAndExpireOrders(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<OrderProcessingDbContext>();
            var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

            // Calculate expiry threshold
            var expiryThreshold = DateTime.UtcNow.AddMinutes(-_settings.OrderExpiryThresholdMinutes);

            // Find orders in processing state older than threshold
            var expiredOrders = await context.Orders
                .Where(o => o.Status == "processing" && o.UpdatedAt < expiryThreshold)
                .ToListAsync(cancellationToken);

            if (expiredOrders.Count == 0)
            {
                _logger.LogDebug("No expired orders found");
                return;
            }

            _logger.LogInformation("Found {Count} expired orders", expiredOrders.Count);

            foreach (var order in expiredOrders)
            {
                // Update status to expired
                order.Status = "expired";
                order.UpdatedAt = DateTime.UtcNow;

                // Publish OrderExpired event
                var expiredEvent = new OrderExpiredEvent
                {
                    OrderId = order.Id,
                    UserId = order.UserId,
                    ExpiredAt = order.UpdatedAt,
                    OriginalCreatedAt = order.CreatedAt
                };

                await publishEndpoint.Publish(expiredEvent, cancellationToken);

                _logger.LogInformation("Order {OrderId} expired and event published", order.Id);
            }

            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Processed {Count} expired orders", expiredOrders.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking and expiring orders");
            // Don't throw - let the timer continue
        }
    }

    public override void Dispose()
    {
        _timer?.Dispose();
        base.Dispose();
    }
}
```

**Implementation Notes:**
- Uses `PeriodicTimer` (.NET 6+) for scheduled execution - modern, efficient timer pattern
- Uses `IServiceProvider.CreateScope()` because BackgroundService is singleton but DbContext must be scoped
- Initial 5-second delay allows database and RabbitMQ to be ready
- Uses `DateTime.UtcNow` for timezone-safe comparisons (matches existing pattern in `OrdersController.cs:98-99`)
- Errors are logged but don't stop the service - resilient design

**Documentation:**
- [PeriodicTimer Documentation](https://learn.microsoft.com/en-us/dotnet/api/system.threading.periodictimer)
- [Background tasks with hosted services](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services)

---

### Phase 8: Docker Orchestration

#### 8.1 Update docker-compose.yml

**Replace `docker-compose.yml` at repository root:**
```yaml
version: '3.8'

services:
  postgres:
    image: postgres:16-alpine
    container_name: orderprocessing-db
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: SecureP@ssw0rd123
      POSTGRES_DB: orderprocessing
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 10s
      timeout: 5s
      retries: 5
    networks:
      - orderprocessing-network

  rabbitmq:
    image: rabbitmq:3-management
    container_name: orderprocessing-rabbitmq
    environment:
      RABBITMQ_DEFAULT_USER: guest
      RABBITMQ_DEFAULT_PASS: guest
    ports:
      - "5672:5672"   # AMQP port
      - "15672:15672" # Management UI
    healthcheck:
      test: ["CMD", "rabbitmq-diagnostics", "-q", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5
    networks:
      - orderprocessing-network

  webapi:
    build:
      context: .
      dockerfile: Dockerfile
    container_name: orderprocessing-api
    depends_on:
      postgres:
        condition: service_healthy
      rabbitmq:
        condition: service_healthy
    ports:
      - "5115:80"
      - "7037:443"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:80
      - ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=orderprocessing;Username=postgres;Password=SecureP@ssw0rd123
    networks:
      - orderprocessing-network
    volumes:
      - ./Configuration:/Configuration:ro

  worker:
    build:
      context: .
      dockerfile: Dockerfile.worker
    container_name: orderprocessing-worker
    depends_on:
      postgres:
        condition: service_healthy
      rabbitmq:
        condition: service_healthy
      webapi:
        condition: service_started
    environment:
      - DOTNET_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=orderprocessing;Username=postgres;Password=SecureP@ssw0rd123
    networks:
      - orderprocessing-network
    volumes:
      - ./Configuration:/Configuration:ro

  cronjob:
    build:
      context: .
      dockerfile: Dockerfile.cronjob
    container_name: orderprocessing-cronjob
    depends_on:
      postgres:
        condition: service_healthy
      rabbitmq:
        condition: service_healthy
      webapi:
        condition: service_started
    environment:
      - DOTNET_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=orderprocessing;Username=postgres;Password=SecureP@ssw0rd123
    networks:
      - orderprocessing-network
    volumes:
      - ./Configuration:/Configuration:ro

volumes:
  postgres_data:

networks:
  orderprocessing-network:
    driver: bridge
```

**Key Changes:**
- Added `rabbitmq` service with management UI
- Worker and CronJob depend on both postgres and rabbitmq being healthy
- All services can access Configuration folder via volume mount
- RabbitMQ management UI available at http://localhost:15672 (guest/guest)

#### 8.2 Create Dockerfile.worker

**Create `Dockerfile.worker` at repository root:**
```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY AI.OrderProcessingSystem.Worker/AI.OrderProcessingSystem.Worker.csproj AI.OrderProcessingSystem.Worker/
COPY AI.OrderProcessingSystem.Common/AI.OrderProcessingSystem.Common.csproj AI.OrderProcessingSystem.Common/
COPY AI.OrderProcessingSystem.Dal/AI.OrderProcessingSystem.Dal.csproj AI.OrderProcessingSystem.Dal/

# Restore dependencies
RUN dotnet restore AI.OrderProcessingSystem.Worker/AI.OrderProcessingSystem.Worker.csproj

# Copy source code
COPY . .

# Build the Worker project
WORKDIR /src/AI.OrderProcessingSystem.Worker
RUN dotnet build AI.OrderProcessingSystem.Worker.csproj -c Release -o /app/build

# Publish
RUN dotnet publish AI.OrderProcessingSystem.Worker.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy published files
COPY --from=build /app/publish .

# Copy configuration files
COPY Configuration /Configuration

# Set environment variable
ENV DOTNET_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "AI.OrderProcessingSystem.Worker.dll"]
```

**Pattern Reference:** Follows `Dockerfile:1-41` multi-stage build pattern.

#### 8.3 Create Dockerfile.cronjob

**Create `Dockerfile.cronjob` at repository root:**
```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY AI.OrderProcessingSystem.CronJob/AI.OrderProcessingSystem.CronJob.csproj AI.OrderProcessingSystem.CronJob/
COPY AI.OrderProcessingSystem.Common/AI.OrderProcessingSystem.Common.csproj AI.OrderProcessingSystem.Common/
COPY AI.OrderProcessingSystem.Dal/AI.OrderProcessingSystem.Dal.csproj AI.OrderProcessingSystem.Dal/

# Restore dependencies
RUN dotnet restore AI.OrderProcessingSystem.CronJob/AI.OrderProcessingSystem.CronJob.csproj

# Copy source code
COPY . .

# Build the CronJob project
WORKDIR /src/AI.OrderProcessingSystem.CronJob
RUN dotnet build AI.OrderProcessingSystem.CronJob.csproj -c Release -o /app/build

# Publish
RUN dotnet publish AI.OrderProcessingSystem.CronJob.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy published files
COPY --from=build /app/publish .

# Copy configuration files
COPY Configuration /Configuration

# Set environment variable
ENV DOTNET_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "AI.OrderProcessingSystem.CronJob.dll"]
```

#### 8.4 Update .dockerignore (if needed)

Ensure `.dockerignore` doesn't exclude Configuration folder (it should already be set up correctly based on existing `.dockerignore:1-135`).

---

### Phase 9: Testing Strategy

#### 9.1 Manual Testing Steps

**1. Start all services:**
```bash
docker-compose up --build
```

**2. Verify all services are healthy:**
```bash
docker-compose ps
```
Expected: All services show "healthy" or "Up" status.

**3. Access RabbitMQ Management UI:**
- URL: http://localhost:15672
- Credentials: guest/guest
- Verify queues are created (should see queues for OrderCreatedEvent, OrderCompletedEvent, OrderExpiredEvent)

**4. Test order creation flow:**
```bash
# Login to get JWT token
curl -X POST http://localhost:5115/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email": "admin@orderprocessing.local", "password": "Admin@12345"}'

# Create an order (replace YOUR_TOKEN)
curl -X POST http://localhost:5115/api/orders \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "userId": 1,
    "items": [{"productId": 1, "quantity": 2}]
  }'
```

**5. Check Worker logs:**
```bash
docker logs orderprocessing-worker -f
```
Expected output:
- "Processing OrderCreated event for Order X"
- After 5 seconds: Either "Order X completed successfully" or "Order X remains in processing state"

**6. Check for email log (if order completed):**
Look for: `📧 SENDING EMAIL: Order #X completed!`

**7. Verify database notifications:**
```bash
docker exec -it orderprocessing-db psql -U postgres -d orderprocessing -c "SELECT * FROM notifications;"
```

**8. Test order expiry (wait 10+ minutes or temporarily change config):**
- Create an order that stays in "processing" state
- Wait for CronJob to run (60 seconds intervals)
- Check CronJob logs: `docker logs orderprocessing-cronjob -f`
- Verify order status changed to "expired"
- Verify OrderExpired notification in database

#### 9.2 Integration Testing

**Update `AI.OrderProcessingSystem.WebApi.Tests\Fixtures\CustomWebApplicationFactory.cs`:**

Add RabbitMQ Testcontainer (requires NuGet package `Testcontainers.RabbitMq`):
```bash
cd AI.OrderProcessingSystem.WebApi.Tests
dotnet add package Testcontainers.RabbitMq
```

Modify fixture to include RabbitMQ container:
```csharp
using Testcontainers.RabbitMq;

private readonly RabbitMqContainer _rabbitMqContainer = new RabbitMqBuilder()
    .WithImage("rabbitmq:3-management")
    .Build();

public async Task InitializeAsync()
{
    await _dbContainer.StartAsync();
    await _rabbitMqContainer.StartAsync();
}

// Update ConfigureWebHost to configure RabbitMQ connection
```

**Create integration test for event flow:**
```csharp
// Test that OrderCreated event is published when order is created
// Test that Worker processes event and updates order status
// Test that notifications are created
```

**Note:** Full integration test implementation is complex due to timing and async nature. Focus on:
1. Unit tests for consumers
2. Integration tests for API endpoints
3. Manual testing for full event flow

---

### Phase 10: Documentation Updates

#### 10.1 Update README.md

**Add to Features section (after line 12):**
```markdown
- Event-driven architecture with RabbitMQ
- Background order processing with Worker Service
- Automated order expiry with scheduled CronJob
- Notification audit trail for all order events
```

**Add to Quick Start section (after docker-compose up):**
```markdown
This will:
- Start PostgreSQL on port 5432
- Start RabbitMQ on ports 5672 (AMQP) and 15672 (Management UI)
- Build and start the WebApi on ports 5115 (HTTP) and 7037 (HTTPS)
- Start the Worker service for background order processing
- Start the CronJob service for order expiry checking
- Automatically run migrations and seed data
```

**Add new section:**
```markdown
### 5. Access RabbitMQ Management UI

- **URL**: http://localhost:15672
- **Credentials**: guest/guest

Use the management UI to monitor message queues, exchanges, and message flow in real-time.

## Event Flow

### Order Processing Flow
1. User creates order via POST /api/orders
2. Order saved with status=pending
3. OrderCreated event published to RabbitMQ
4. Worker consumes event and updates order to processing
5. Worker simulates 5-second payment delay
6. 50% chance: Order completed → OrderCompleted event published
7. 50% chance: Order remains in processing state

### Order Completion Flow
1. Worker publishes OrderCompleted event
2. Worker (different consumer) receives event
3. Fake email logged to console
4. Notification record created in database

### Order Expiry Flow
1. CronJob runs every 60 seconds
2. Finds orders in processing state older than 10 minutes
3. Updates order status to expired
4. Publishes OrderExpired event
5. Worker receives event and creates notification record

## Configuration

Event processing behavior is configurable via `\Configuration\instance.json`:

- `PaymentProcessingDelaySeconds`: Time to simulate payment (default: 5)
- `OrderCompletionSuccessRate`: Probability of order completion (default: 0.5 = 50%)
- `OrderExpiryThresholdMinutes`: Age threshold for expiry (default: 10)
- `ExpiryCheckIntervalSeconds`: How often CronJob runs (default: 60)
```

**Update Troubleshooting section:**
```markdown
**RabbitMQ connection issues**:
```bash
# Check RabbitMQ logs
docker logs orderprocessing-rabbitmq

# Restart RabbitMQ
docker-compose restart rabbitmq
```

**Worker or CronJob not processing events**:
```bash
# Check Worker logs
docker logs orderprocessing-worker -f

# Check CronJob logs
docker logs orderprocessing-cronjob -f

# Verify RabbitMQ queues exist
# Open http://localhost:15672 and check Queues tab
```
```

#### 10.2 Create ARCHITECTURE.md (Optional but Recommended)

**Create `ARCHITECTURE.md` at repository root:**
```markdown
# Architecture Overview

## System Components

### WebApi (AI.OrderProcessingSystem.WebApi)
- ASP.NET Core MVC application
- REST API endpoints for orders, products, users
- JWT authentication
- Publishes domain events to RabbitMQ
- Runs database migrations on startup

### Worker (AI.OrderProcessingSystem.Worker)
- .NET Worker Service (BackgroundService)
- Consumes events from RabbitMQ:
  - OrderCreatedEvent → Simulates payment processing
  - OrderCompletedEvent → Sends email and creates notification
  - OrderExpiredEvent → Creates notification
- Scoped DbContext per message

### CronJob (AI.OrderProcessingSystem.CronJob)
- .NET Worker Service (BackgroundService with PeriodicTimer)
- Runs every 60 seconds
- Finds and expires old processing orders
- Publishes OrderExpired events

### Dal (AI.OrderProcessingSystem.Dal)
- Data Access Layer
- Entity Framework Core entities and DbContext
- PostgreSQL database provider
- Migrations

### Common (AI.OrderProcessingSystem.Common)
- Shared DTOs, events, enums, configuration classes
- No external project dependencies (foundation layer)

## Event Flow Diagram

```
[User] → POST /api/orders → [WebApi]
                              ↓
                        Save to DB (status=pending)
                              ↓
                        Publish OrderCreatedEvent
                              ↓
                          [RabbitMQ]
                              ↓
                          [Worker]
                              ↓
                    Update status=processing
                              ↓
                    Simulate payment (5s)
                              ↓
                    ┌─────────┴─────────┐
                    ↓                   ↓
              50% Success         50% Fail
                    ↓                   ↓
           status=completed      status=processing
                    ↓                   ↓
        Publish OrderCompleted    (stays in queue)
                    ↓                   ↓
              [RabbitMQ]          [CronJob checks]
                    ↓                   ↓
              [Worker]            After 10 min
                    ↓                   ↓
         Log email + save         status=expired
           notification               ↓
                              Publish OrderExpired
                                      ↓
                                 [RabbitMQ]
                                      ↓
                                  [Worker]
                                      ↓
                              Save notification
```

## Technology Stack

- **.NET 8**: Runtime and SDK
- **ASP.NET Core**: Web API framework
- **Entity Framework Core 8**: ORM
- **PostgreSQL 16**: Database
- **RabbitMQ 3**: Message broker
- **MassTransit**: .NET messaging abstraction
- **Docker & Docker Compose**: Containerization

## Project Dependencies

```
Common (no dependencies)
  ↑
  ├── Dal
  │    ↑
  │    ├── WebApi
  │    ├── Worker
  │    └── CronJob
  │
  ├── WebApi
  ├── Worker
  └── CronJob
```

Forbidden dependencies (enforced by architecture rules):
- WebApi ❌ Worker, CronJob
- Worker ❌ WebApi, CronJob
- CronJob ❌ WebApi, Worker
```

---

## Implementation Task Checklist

Execute tasks in this exact order for one-pass implementation success:

### Phase 1: Configuration Foundation
- [ ] Create `\Configuration\` folder at repository root
- [ ] Create `\Configuration\instance.json` with EventProcessingSettings and RabbitMqSettings
- [ ] Create `\Configuration\secrets.json` with RabbitMQ credentials
- [ ] Create `Common\Configuration\EventProcessingSettings.cs`
- [ ] Create `Common\Configuration\RabbitMqSettings.cs`
- [ ] Update `WebApi\Configuration\AppConfiguration.cs` with new properties
- [ ] Update `WebApi\Program.cs` configuration loading and validation
- [ ] Build and verify WebApi starts without errors

### Phase 2: Event Contracts
- [ ] Create `Common\Events\OrderCreatedEvent.cs`
- [ ] Create `Common\Events\OrderCompletedEvent.cs`
- [ ] Create `Common\Events\OrderExpiredEvent.cs`
- [ ] Create `Common\Abstractions\IEventPublisher.cs`
- [ ] Build Common project

### Phase 3: Database Schema
- [ ] Create `Dal\Entities\Notification.cs`
- [ ] Create `Common\Enums\EventType.cs`
- [ ] Update `Dal\Data\OrderProcessingDbContext.cs` with Notifications DbSet
- [ ] Add Notification entity configuration in `OnModelCreating`
- [ ] Run migration: `dotnet ef migrations add AddNotificationsTable`
- [ ] Build Dal project

### Phase 4: MassTransit Infrastructure
- [ ] Add MassTransit NuGet packages to WebApi
- [ ] Add MassTransit + other packages to Worker
- [ ] Add MassTransit + other packages to CronJob
- [ ] Add project references to Worker (Common, Dal)
- [ ] Add project references to CronJob (Common, Dal)
- [ ] Create `WebApi\Services\MassTransitEventPublisher.cs`
- [ ] Update `WebApi\Program.cs` to register MassTransit and IEventPublisher
- [ ] Build all projects

### Phase 5: WebApi Event Publishing
- [ ] Update `WebApi\Controllers\OrdersController.cs` constructor with IEventPublisher
- [ ] Add event publishing after SaveChangesAsync in Create method
- [ ] Add using statements for Events and Abstractions
- [ ] Build and test WebApi locally

### Phase 6: Worker Service
- [ ] Replace `Worker\Program.cs` with full Worker Service implementation
- [ ] Create `Worker\Consumers\OrderCreatedConsumer.cs`
- [ ] Create `Worker\Consumers\OrderCompletedConsumer.cs`
- [ ] Create `Worker\Consumers\OrderExpiredConsumer.cs`
- [ ] Build Worker project
- [ ] Test Worker locally (with local RabbitMQ and PostgreSQL)

### Phase 7: CronJob Service
- [ ] Replace `CronJob\Program.cs` with full Worker Service implementation
- [ ] Create `CronJob\Services\OrderExpiryService.cs`
- [ ] Build CronJob project
- [ ] Test CronJob locally

### Phase 8: Docker Orchestration
- [ ] Update `docker-compose.yml` with RabbitMQ service
- [ ] Add Worker and CronJob services to docker-compose.yml
- [ ] Create `Dockerfile.worker`
- [ ] Create `Dockerfile.cronjob`
- [ ] Verify `.dockerignore` allows Configuration folder
- [ ] Test: `docker-compose up --build`
- [ ] Verify all services start healthy

### Phase 9: Testing
- [ ] Manual test: Create order via Swagger/curl
- [ ] Verify Worker logs show event processing
- [ ] Verify order status changes (pending → processing → completed/processing)
- [ ] Verify notifications table has records
- [ ] Verify email logged for completed orders
- [ ] Test expiry: Create order and wait for CronJob (or adjust config)
- [ ] Verify expired orders and notifications
- [ ] Check RabbitMQ Management UI for queue activity

### Phase 10: Documentation
- [ ] Update README.md with event flow, RabbitMQ access, new features
- [ ] Optionally create ARCHITECTURE.md
- [ ] Update CLAUDE.md if needed with new project states

---

## Validation Gates

Execute these commands to validate implementation:

### Code Quality
```bash
# Format code
dotnet format AI.OrderProcessingSystem.sln

# Build with warnings as errors
dotnet build AI.OrderProcessingSystem.sln -warnaserror

# Restore and build all projects
dotnet restore AI.OrderProcessingSystem.sln
dotnet build AI.OrderProcessingSystem.sln --configuration Release
```

### Testing
```bash
# Run all tests
dotnet test AI.OrderProcessingSystem.WebApi.Tests --configuration Release --verbosity normal

# Run with coverage (if configured)
dotnet test AI.OrderProcessingSystem.WebApi.Tests --collect:"XPlat Code Coverage"
```

### Docker Validation
```bash
# Build all Docker images
docker-compose build

# Start all services
docker-compose up -d

# Check service health
docker-compose ps

# Check logs for errors
docker-compose logs webapi
docker-compose logs worker
docker-compose logs cronjob
docker-compose logs rabbitmq

# Stop all services
docker-compose down
```

### Runtime Validation
```bash
# Access Swagger UI
# Open: http://localhost:5115/swagger

# Access RabbitMQ Management
# Open: http://localhost:15672 (guest/guest)

# Check database
docker exec -it orderprocessing-db psql -U postgres -d orderprocessing
# \dt - list tables (should see notifications)
# SELECT * FROM notifications; - verify records
```

---

## Common Pitfalls & Gotchas

### 1. Configuration File Path Issues
**Problem:** Worker/CronJob can't find Configuration folder
**Solution:** Ensure path is `Path.Combine(Directory.GetCurrentDirectory(), "..", "Configuration", ...)` and Configuration folder is mounted in docker-compose volumes

### 2. DbContext Lifetime in BackgroundService
**Problem:** Cannot inject scoped DbContext into singleton BackgroundService
**Solution:** Use `IServiceProvider.CreateScope()` within ExecuteAsync method (already implemented in OrderExpiryService)

### 3. RabbitMQ Connection on Startup
**Problem:** Worker/CronJob start before RabbitMQ is ready
**Solution:** Use `depends_on` with `condition: service_healthy` in docker-compose.yml (already configured)

### 4. Migration Timing
**Problem:** Worker/CronJob try to access Notifications table before migration runs
**Solution:** Only WebApi runs migrations. Worker and CronJob depend on WebApi in docker-compose (already configured with `depends_on: webapi`)

### 5. Event Publishing After SaveChanges Failure
**Problem:** If SaveChanges succeeds but Publish fails, event is lost
**Solution:** For production, implement Transactional Outbox pattern. For this implementation, rely on MassTransit retry policies.

### 6. Timezone Issues in Expiry Logic
**Problem:** Server time vs UTC mismatch causes incorrect expiry
**Solution:** Always use `DateTime.UtcNow` (already implemented consistently across codebase)

### 7. Random Seed in OrderCreatedConsumer
**Problem:** Creating new Random() in consumer method can cause predictable patterns
**Solution:** Use `Random.Shared` (.NET 6+) or inject singleton Random. Current implementation is acceptable for demo but note for production.

### 8. Message Duplication
**Problem:** RabbitMQ may deliver same message twice (network issues, retries)
**Solution:** Implement idempotency checks (e.g., check if order status already changed before processing). Not implemented in this PRP but worth noting.

---

## Success Criteria

Implementation is complete and successful when:

1. ✅ All projects build without warnings or errors
2. ✅ All validation gates pass (build, format, tests)
3. ✅ `docker-compose up` starts all 5 services (postgres, rabbitmq, webapi, worker, cronjob) healthy
4. ✅ Creating an order via API:
   - Saves order with status=pending
   - Publishes OrderCreatedEvent (visible in RabbitMQ Management UI)
   - Worker consumes event and updates to processing
   - After 5 seconds, ~50% complete with notification, ~50% stay processing
5. ✅ Completed orders trigger:
   - Email log in Worker console
   - Notification record in database with is_email_sent=true
6. ✅ CronJob runs every 60 seconds and:
   - Finds processing orders older than 10 minutes
   - Updates to expired
   - Publishes OrderExpired event
   - Creates notification record
7. ✅ RabbitMQ Management UI shows:
   - Queues for all three event types
   - Messages being published and consumed
8. ✅ Database contains:
   - Notifications table with proper schema
   - Notification records for completed and expired orders
9. ✅ README.md updated with event flow documentation
10. ✅ Manual testing confirms end-to-end flow works as specified

---

## External Resources

### MassTransit Documentation
- [MassTransit with RabbitMQ - Milan Jovanovic](https://www.milanjovanovic.tech/blog/using-masstransit-with-rabbitmq-and-azure-service-bus)
- [RabbitMQ Configuration · MassTransit](https://masstransit.io/documentation/configuration/transports/rabbitmq)
- [MassTransit Quick Start](https://masstransit.io/quick-starts/rabbitmq)
- [Using MassTransit in ASP.NET Core - Code Maze](https://code-maze.com/masstransit-rabbitmq-aspnetcore/)
- [RabbitMQ and MassTransit in .NET Core - Practical Guide](https://hamedsalameh.com/rabbitmq-and-masstransit-in-net-core-practical-guide/)
- [Building Event Driven .NET Apps with MassTransit - Wrapt](https://wrapt.dev/blog/building-an-event-driven-dotnet-application-setting-up-masstransit-and-rabbitmq)

### .NET Worker Services
- [Worker Services - .NET Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/extensions/workers)
- [Background tasks with hosted services in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services)
- [PeriodicTimer Documentation](https://learn.microsoft.com/en-us/dotnet/api/system.threading.periodictimer)
- [Mastering Background Jobs in .NET 9 with Worker Services](https://medium.com/@vahidbakhtiaryinfo/mastering-background-jobs-in-net-9-with-worker-services-and-channels-a5766475e869)

### RabbitMQ
- [RabbitMQ Docker Hub](https://hub.docker.com/_/rabbitmq)
- [RabbitMQ .NET Client Documentation](https://www.rabbitmq.com/client-libraries/dotnet-api-guide)

### Testing
- [Testcontainers for .NET](https://dotnet.testcontainers.org/)
- [Integration testing in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests)

---

## Estimated Implementation Time

Based on task complexity and dependencies:

- **Phase 1-3** (Configuration, Events, Database): 1-2 hours
- **Phase 4-5** (MassTransit, WebApi): 1 hour
- **Phase 6** (Worker Service): 1.5 hours
- **Phase 7** (CronJob Service): 1 hour
- **Phase 8** (Docker): 1 hour
- **Phase 9** (Testing & Debugging): 1-2 hours
- **Phase 10** (Documentation): 0.5 hours

**Total: 7-10 hours** for experienced developer

**Confidence: 8/10** - Well-defined tasks with clear patterns, but Docker orchestration and timing issues could require debugging.

---

*End of PRP*
