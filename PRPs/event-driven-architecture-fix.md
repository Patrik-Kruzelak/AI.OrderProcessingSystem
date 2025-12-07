# PRP: Fix Event-Driven Order Processing Flow

## Overview

This PRP addresses critical issues in the event-driven order processing workflow. The current implementation has incorrect event publishing, missing business logic in consumers, and unnecessary event handling that needs to be removed.

### What This PRP Accomplishes

1. ✅ Implements proper payment processing simulation in `OrderCreatedConsumer`
2. ✅ Adds 50% random success/failure logic for order completion
3. ✅ Implements fake email notification in `OrderCompletedConsumer`
4. ✅ Fixes CRON job to expire "processing" orders instead of "pending"
5. ✅ Removes `OrderExpiredEvent` and its consumer
6. ✅ Removes incorrect event publishing from `OrdersController`
7. ✅ Updates configuration values to match requirements

### Why This Matters

The event-driven architecture is currently incomplete. Orders are created but not properly processed through the workflow. This PRP completes the implementation to enable:
- Automated payment processing simulation
- Proper order lifecycle management (pending → processing → completed/expired)
- Notification delivery for completed orders
- Automatic expiration of stale orders

---

## Current State Analysis

### What's Working ✓

1. **Order Creation** (`AI.OrderProcessingSystem.WebApi\Controllers\OrdersController.cs:65-144`)
   - Creates orders with status = "pending"
   - Publishes `OrderCreatedEvent` correctly (line 122)
   - Saves initial notification to database (lines 125-134)

2. **CRON Job Structure** (`AI.OrderProcessingSystem.CronJob\Services\OrderExpiryService.cs:13-104`)
   - Runs on configured interval using `PeriodicTimer`
   - Updates order status to "expired"
   - Creates notification records

3. **Event Infrastructure**
   - MassTransit configured in all projects
   - RabbitMQ message broker setup
   - `IEventPublisher` abstraction with working implementation

### What's Broken ✗

1. **OrderCreatedConsumer** (`AI.OrderProcessingSystem.Worker\Consumers\OrderCreatedConsumer.cs:16-28`)
   - Currently only logs the event (line 20-22)
   - **MISSING**: Status update to "processing"
   - **MISSING**: Payment processing delay
   - **MISSING**: 50% success rate logic
   - **MISSING**: Database access and event publishing

2. **OrderCompletedConsumer** (`AI.OrderProcessingSystem.Worker\Consumers\OrderCompletedConsumer.cs:16-28`)
   - Currently only logs the event (line 20-22)
   - **MISSING**: Fake email sending logic
   - **MISSING**: Database notification save

3. **OrdersController Event Publishing** (`OrdersController.cs:175-198`)
   - **INCORRECT**: Publishes `OrderCompletedEvent` during manual status update
   - This should ONLY happen in `OrderCreatedConsumer`

4. **CRON Job Logic** (`OrderExpiryService.cs:59`)
   - **INCORRECT**: Checks for "pending" orders
   - Should check for "processing" orders

5. **OrderExpiredEvent Flow**
   - **UNNECESSARY**: Event and consumer exist but shouldn't
   - Event: `AI.OrderProcessingSystem.Common\Events\OrderExpiredEvent.cs`
   - Consumer: `AI.OrderProcessingSystem.Worker\Consumers\OrderExpiredConsumer.cs:7-29`
   - Registration: `AI.OrderProcessingSystem.Worker\Program.cs:44` and `65-68`

6. **Configuration Values** (`Configuration\instance.json`)
   - `PaymentProcessingDelaySeconds`: 1 (should be 5)
   - `OrderCompletionSuccessRate`: 0.8 (should be 0.5)
   - `ExpiryCheckIntervalSeconds`: 30 (should be 60)
   - `OrderExpiryThresholdMinutes`: 5 (should be 10)

---

## Required Changes

### 1. Configuration Updates

**File**: `Configuration\instance.json`

Update these values:

```json
{
  "EventProcessingSettings": {
    "PaymentProcessingDelaySeconds": 5,        // Changed from 1
    "OrderCompletionSuccessRate": 0.5,         // Changed from 0.8
    "OrderExpiryThresholdMinutes": 10,         // Changed from 5
    "ExpiryCheckIntervalSeconds": 60           // Changed from 30
  }
}
```

### 2. OrderCreatedConsumer - Complete Rewrite

**File**: `AI.OrderProcessingSystem.Worker\Consumers\OrderCreatedConsumer.cs`

**Current**: Only logs events
**Required**: Full payment processing simulation

**New Dependencies Needed**:
- `OrderProcessingDbContext` - for database operations
- `IEventPublisher` - for publishing `OrderCompletedEvent`
- `EventProcessingSettings` - for configuration values
- `IServiceProvider` - for creating scoped DbContext

**Business Logic Flow**:
```
1. Receive OrderCreatedEvent
2. Create scoped DbContext
3. Load order from database by OrderId
4. Update order status: "pending" → "processing"
5. Save changes to database
6. Log: "Order {OrderId} status changed to processing"
7. Delay for PaymentProcessingDelaySeconds (5 seconds)
8. Generate random number (0.0 to 1.0)
9. If random < OrderCompletionSuccessRate (0.5):
   a. Update order status: "processing" → "completed"
   b. Set order.UpdatedAt = DateTime.UtcNow
   c. Save changes to database
   d. Publish OrderCompletedEvent
   e. Log: "Order {OrderId} completed successfully"
10. Else:
   a. Log: "Order {OrderId} payment failed, remaining in processing"
   b. No status change, no event published
```

**Pattern Reference**: See `OrderExpiryService.cs:50-103` for scoped DbContext pattern

**Random Number Generation**: Use `Random.Shared.NextDouble()` (.NET 6+) for thread-safety

### 3. OrderCompletedConsumer - Add Email Logic

**File**: `AI.OrderProcessingSystem.Worker\Consumers\OrderCompletedConsumer.cs`

**Current**: Only logs events
**Required**: Fake email sending + notification save

**New Dependencies Needed**:
- `OrderProcessingDbContext` - for saving notifications
- `IServiceProvider` - for creating scoped DbContext

**Business Logic Flow**:
```
1. Receive OrderCompletedEvent
2. Log fake email: "Sending email for completed order {OrderId} to user {UserId}"
3. Log fake email: "✉️ Email sent successfully for order {OrderId}"
4. Create scoped DbContext
5. Create Notification entity:
   - OrderId = message.OrderId
   - EventType = "OrderCompleted"
   - Message = "Order #{OrderId} completed successfully"
   - IsEmailSent = true  (since we "sent" the fake email)
   - CreatedAt = DateTime.UtcNow
6. Add notification to context
7. Save changes to database
8. Log: "Notification saved for order {OrderId}"
```

**Pattern Reference**: See `OrderExpiryService.cs:87-95` for notification creation pattern

### 4. Remove OrderCompleted Publishing from OrdersController

**File**: `AI.OrderProcessingSystem.WebApi\Controllers\OrdersController.cs`

**Lines to Remove**: 175-198

Remove this entire block:
```csharp
// Publish OrderCompletedEvent if status changed to completed
if (oldStatus != "completed" && order.Status == "completed")
{
    var orderCompletedEvent = new OrderCompletedEvent
    {
        OrderId = order.Id,
        UserId = order.UserId,
        Total = order.Total,
        CompletedAt = order.UpdatedAt
    };
    await _eventPublisher.PublishAsync(orderCompletedEvent);

    // Create notification
    var notification = new Notification
    {
        OrderId = order.Id,
        EventType = "OrderCompleted",
        Message = $"Order #{order.Id} completed successfully",
        IsEmailSent = false,
        CreatedAt = DateTime.UtcNow
    };
    _context.Notifications.Add(notification);
    await _context.SaveChangesAsync();
}
```

**Reason**: Order completion should ONLY happen via the event-driven flow, not manual updates.

### 5. Fix CRON Job Status Filter

**File**: `AI.OrderProcessingSystem.CronJob\Services\OrderExpiryService.cs`

**Line 59**: Change from "pending" to "processing"

```csharp
// Before:
.Where(o => o.Status == "pending" && o.CreatedAt < expiryThreshold)

// After:
.Where(o => o.Status == "processing" && o.CreatedAt < expiryThreshold)
```

**Reason**: Only orders stuck in "processing" should expire. "Pending" orders haven't started processing yet.

### 6. Remove OrderExpiredEvent Publishing from CRON Job

**File**: `AI.OrderProcessingSystem.CronJob\Services\OrderExpiryService.cs`

**Lines to Remove**: 76-84

Remove this block:
```csharp
// Publish OrderExpiredEvent
var orderExpiredEvent = new OrderExpiredEvent
{
    OrderId = order.Id,
    UserId = order.UserId,
    ExpiredAt = order.UpdatedAt
};

await eventPublisher.PublishAsync(orderExpiredEvent, cancellationToken);
```

**Keep**: The notification creation (lines 87-95) - this is still needed

### 7. Delete OrderExpiredEvent Event Class

**File to Delete**: `AI.OrderProcessingSystem.Common\Events\OrderExpiredEvent.cs`

### 8. Delete OrderExpiredConsumer

**File to Delete**: `AI.OrderProcessingSystem.Worker\Consumers\OrderExpiredConsumer.cs`

### 9. Remove OrderExpiredConsumer Registration

**File**: `AI.OrderProcessingSystem.Worker\Program.cs`

**Line 44**: Remove `x.AddConsumer<OrderExpiredConsumer>();`

**Lines 65-68**: Remove this entire block:
```csharp
cfg.ReceiveEndpoint("order-expired-queue", e =>
{
    e.ConfigureConsumer<OrderExpiredConsumer>(context);
});
```

---

## Implementation Plan

Execute these tasks **in order**:

### Phase 1: Configuration & Cleanup (Low Risk)

1. ✅ **Update Configuration Values**
   - Edit `Configuration\instance.json`
   - Update all 4 values in `EventProcessingSettings`
   - Verify JSON is valid

2. ✅ **Remove OrderExpiredEvent Event Class**
   - Delete file: `AI.OrderProcessingSystem.Common\Events\OrderExpiredEvent.cs`

3. ✅ **Remove OrderExpiredConsumer**
   - Delete file: `AI.OrderProcessingSystem.Worker\Consumers\OrderExpiredConsumer.cs`

4. ✅ **Remove Consumer Registration**
   - Edit `AI.OrderProcessingSystem.Worker\Program.cs`
   - Remove line 44: `x.AddConsumer<OrderExpiredConsumer>();`
   - Remove lines 65-68: `order-expired-queue` endpoint configuration
   - Remove using statement if no longer needed

### Phase 2: Fix CRON Job (Medium Risk)

5. ✅ **Fix OrderExpiryService Status Filter**
   - Edit `AI.OrderProcessingSystem.CronJob\Services\OrderExpiryService.cs:59`
   - Change filter from "pending" to "processing"

6. ✅ **Remove Event Publishing from CRON Job**
   - Edit `OrderExpiryService.cs`
   - Remove lines 76-84 (OrderExpiredEvent publishing)
   - Keep notification creation (lines 87-95)

### Phase 3: Fix Controllers (Medium Risk)

7. ✅ **Remove Incorrect Event Publishing from OrdersController**
   - Edit `AI.OrderProcessingSystem.WebApi\Controllers\OrdersController.cs`
   - Remove lines 175-198 (entire if block for OrderCompleted)
   - Keep the status update logic (lines 167-173)
   - Keep the return statement (line 200)

### Phase 4: Implement Consumer Logic (High Risk - Core Business Logic)

8. ✅ **Rewrite OrderCreatedConsumer**
   - Edit `AI.OrderProcessingSystem.Worker\Consumers\OrderCreatedConsumer.cs`
   - Add constructor dependencies: `IServiceProvider`, `EventProcessingSettings`
   - Implement full payment processing flow (see detailed spec above)
   - Use scoped DbContext pattern
   - Implement random success logic with `Random.Shared.NextDouble()`
   - Publish `OrderCompletedEvent` on success

9. ✅ **Enhance OrderCompletedConsumer**
   - Edit `AI.OrderProcessingSystem.Worker\Consumers\OrderCompletedConsumer.cs`
   - Add constructor dependency: `IServiceProvider`
   - Add fake email logging
   - Add notification database save logic
   - Use scoped DbContext pattern

### Phase 5: Register Dependencies in Worker

10. ✅ **Update Worker Program.cs**
    - Edit `AI.OrderProcessingSystem.Worker\Program.cs`
    - Add after RabbitMQ config (line 32):
      ```csharp
      // Build EventProcessingSettings
      var eventProcessingSettings = JsonSerializer.Deserialize<EventProcessingSettings>(
          instanceConfig!["EventProcessingSettings"].GetRawText())!;

      // Build connection string
      var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                             ?? secretsConfig!["ConnectionStrings"].GetProperty("DefaultConnection").GetString()!;

      // Register configuration
      builder.Services.AddSingleton(eventProcessingSettings);

      // Add DbContext
      builder.Services.AddDbContext<OrderProcessingDbContext>(options =>
          options.UseNpgsql(connectionString));

      // Add IEventPublisher
      builder.Services.AddScoped<IEventPublisher, MassTransitEventPublisher>();
      ```

11. ✅ **Add MassTransitEventPublisher to Worker**
    - Create file: `AI.OrderProcessingSystem.Worker\Services\MassTransitEventPublisher.cs`
    - Copy implementation from `AI.OrderProcessingSystem.WebApi\Services\MassTransitEventPublisher.cs`
    - Or add reference to move to Common project if preferred

### Phase 6: Verification

12. ✅ **Build Solution**
    - Run: `dotnet build AI.OrderProcessingSystem.sln`
    - Fix any compilation errors

13. ✅ **Run Tests**
    - Run: `dotnet test AI.OrderProcessingSystem.WebApi.Tests`
    - Verify all tests pass

14. ✅ **Manual Testing**
    - Start RabbitMQ (via Docker)
    - Start WebApi: `cd AI.OrderProcessingSystem.WebApi && dotnet run`
    - Start Worker: `cd AI.OrderProcessingSystem.Worker && dotnet run`
    - Start CronJob: `cd AI.OrderProcessingSystem.CronJob && dotnet run`
    - Create test order via API
    - Verify workflow: pending → processing → completed (50%) or stays processing (50%)
    - Verify fake email log appears for completed orders
    - Verify processing orders expire after 10 minutes

---

## Technical Context

### MassTransit Event Consumer Pattern

**Interface**: `IConsumer<TEvent>`

**Standard Pattern**:
```csharp
public class MyConsumer : IConsumer<MyEvent>
{
    private readonly ILogger<MyConsumer> _logger;
    private readonly IServiceProvider _serviceProvider;

    public MyConsumer(ILogger<MyConsumer> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task Consume(ConsumeContext<MyEvent> context)
    {
        var message = context.Message;

        // Create scope for scoped services
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderProcessingDbContext>();

        // Business logic here

        await dbContext.SaveChangesAsync();
    }
}
```

**Why IServiceProvider?**
- Consumers are registered as **singletons** in MassTransit
- DbContext must be **scoped** (not singleton)
- Use `IServiceProvider.CreateScope()` to get scoped instances

**Reference**: See `OrderExpiryService.cs:52-54` for this exact pattern

### Database Operations in Consumers

**Loading Entities**:
```csharp
var order = await context.Orders
    .FirstOrDefaultAsync(o => o.Id == message.OrderId);

if (order == null)
{
    _logger.LogWarning("Order {OrderId} not found", message.OrderId);
    return; // Skip processing
}
```

**Updating Entities**:
```csharp
order.Status = "processing";
order.UpdatedAt = DateTime.UtcNow;
await context.SaveChangesAsync();
```

**Creating Notifications**:
```csharp
var notification = new Notification
{
    OrderId = order.Id,
    EventType = "OrderCompleted",
    Message = $"Order #{order.Id} completed successfully",
    IsEmailSent = true,
    CreatedAt = DateTime.UtcNow
};
context.Notifications.Add(notification);
await context.SaveChangesAsync();
```

### Event Publishing from Consumers

**Pattern**:
```csharp
// After modifying order in database
var completedEvent = new OrderCompletedEvent
{
    OrderId = order.Id,
    UserId = order.UserId,
    Total = order.Total,
    CompletedAt = order.UpdatedAt
};

await _eventPublisher.PublishAsync(completedEvent);
```

**Important**: Publish events AFTER database save to ensure consistency

### Random Number Generation for Success Rate

**Thread-Safe Approach** (.NET 6+):
```csharp
double randomValue = Random.Shared.NextDouble(); // Returns 0.0 to 1.0

if (randomValue < _eventSettings.OrderCompletionSuccessRate) // 0.5 = 50%
{
    // Success path
}
else
{
    // Failure path
}
```

**Why Random.Shared?**
- Thread-safe (important for concurrent consumers)
- No need to create Random instance
- Available in .NET 6+

### Configuration Access

**In Consumers**:
```csharp
public class OrderCreatedConsumer : IConsumer<OrderCreatedEvent>
{
    private readonly EventProcessingSettings _eventSettings;

    public OrderCreatedConsumer(EventProcessingSettings eventSettings)
    {
        _eventSettings = eventSettings;
    }

    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        int delaySeconds = _eventSettings.PaymentProcessingDelaySeconds;
        await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
    }
}
```

**Registration in Program.cs**:
```csharp
var eventSettings = JsonSerializer.Deserialize<EventProcessingSettings>(
    instanceConfig!["EventProcessingSettings"].GetRawText())!;

builder.Services.AddSingleton(eventSettings);
```

### Valid Order Status Values

As defined in `OrdersController.cs:163`:
- `"pending"` - Order created, not yet processing
- `"processing"` - Payment being processed
- `"completed"` - Payment successful
- `"expired"` - Processing took too long

### Logging Best Practices

**Structured Logging**:
```csharp
_logger.LogInformation(
    "Order {OrderId} status changed from {OldStatus} to {NewStatus}",
    order.Id, "pending", "processing");
```

**Fake Email Logging**:
```csharp
_logger.LogInformation("✉️ Sending email to user {UserId} for order {OrderId}", userId, orderId);
_logger.LogInformation("✉️ Email sent successfully for order {OrderId}", orderId);
```

---

## Validation Gates

Execute these commands to verify implementation:

### 1. Code Formatting
```bash
dotnet format AI.OrderProcessingSystem.sln --verify-no-changes
```

**Expected**: No formatting issues

### 2. Build with Warnings as Errors
```bash
dotnet build AI.OrderProcessingSystem.sln -warnaserror
```

**Expected**: Build succeeds with zero warnings

### 3. Run Unit Tests
```bash
dotnet test AI.OrderProcessingSystem.WebApi.Tests --configuration Release --no-build --verbosity normal
```

**Expected**: All existing tests pass

### 4. Verify Configuration
```bash
# Validate JSON syntax
Get-Content "Configuration\instance.json" | ConvertFrom-Json
```

**Expected**: No JSON parsing errors

**Verify Values**:
- `PaymentProcessingDelaySeconds` = 5
- `OrderCompletionSuccessRate` = 0.5
- `OrderExpiryThresholdMinutes` = 10
- `ExpiryCheckIntervalSeconds` = 60

### 5. Verify File Deletions
```bash
# These files should NOT exist
Test-Path "AI.OrderProcessingSystem.Common\Events\OrderExpiredEvent.cs"  # Should be False
Test-Path "AI.OrderProcessingSystem.Worker\Consumers\OrderExpiredConsumer.cs"  # Should be False
```

### 6. Manual Integration Test

**Prerequisites**:
- PostgreSQL running (docker-compose up -d postgres)
- RabbitMQ running (docker-compose up -d rabbitmq)

**Test Steps**:
1. Start WebApi: `cd AI.OrderProcessingSystem.WebApi && dotnet run`
2. Start Worker: `cd AI.OrderProcessingSystem.Worker && dotnet run`
3. Start CronJob: `cd AI.OrderProcessingSystem.CronJob && dotnet run`
4. Authenticate and get JWT token
5. Create test order via POST /api/orders
6. Wait 5 seconds
7. Check order status via GET /api/orders/{id}
8. Expected results (run multiple times):
   - ~50% of orders: status = "completed"
   - ~50% of orders: status = "processing"
9. For completed orders:
   - Worker logs should show "✉️ Email sent" message
   - Database should have notification with EventType = "OrderCompleted" and IsEmailSent = true
10. For processing orders:
   - Wait 10 minutes
   - CronJob should expire them (status = "expired")

**Expected Logs**:

WebApi:
```
Order created with ID 123
Published event OrderCreatedEvent: ...
```

Worker (OrderCreatedConsumer):
```
Processing OrderCreatedEvent: OrderId=123
Order 123 status changed to processing
[5 second delay]
Order 123 completed successfully  // OR: Order 123 payment failed, remaining in processing
Published event OrderCompletedEvent: ...  // Only if completed
```

Worker (OrderCompletedConsumer - only for completed orders):
```
Processing OrderCompletedEvent: OrderId=123
✉️ Sending email to user X for order 123
✉️ Email sent successfully for order 123
Notification saved for order 123
```

CronJob (after 10 minutes for processing orders):
```
Found 1 expired orders
Order 123 marked as expired
```

---

## Testing Strategy

### Unit Tests to Add

**File**: `AI.OrderProcessingSystem.WebApi.Tests\Tests\ConsumerTests.cs` (new file)

**Test Cases**:

1. ✅ `OrderCreatedConsumer_ProcessesOrder_UpdatesStatusToProcessing`
   - Mock: DbContext with test order
   - Assert: Order status changes to "processing"
   - Assert: UpdatedAt is set

2. ✅ `OrderCreatedConsumer_SuccessfulPayment_CompletesOrder`
   - Mock: Random to return < 0.5 (success)
   - Assert: Order status = "completed"
   - Assert: OrderCompletedEvent published

3. ✅ `OrderCreatedConsumer_FailedPayment_RemainsProcessing`
   - Mock: Random to return > 0.5 (failure)
   - Assert: Order status = "processing"
   - Assert: No event published

4. ✅ `OrderCompletedConsumer_ProcessesEvent_SavesNotification`
   - Mock: DbContext
   - Assert: Notification created with EventType = "OrderCompleted"
   - Assert: IsEmailSent = true

5. ✅ `OrderExpiryService_ExpiresProcessingOrders_NotPendingOrders`
   - Create 1 pending order, 1 processing order (both > 10 min old)
   - Assert: Only processing order is expired
   - Assert: Pending order remains pending

**Testing Pattern**: Use xUnit with `CustomWebApplicationFactory` pattern

**Mock Pattern**: Use Moq or NSubstitute for dependencies

### Integration Tests

**Existing Tests to Verify**:
- All tests in `AI.OrderProcessingSystem.WebApi.Tests` should still pass
- No breaking changes to API contracts

**New Integration Test** (optional but recommended):

**File**: `AI.OrderProcessingSystem.WebApi.Tests\Tests\OrderWorkflowIntegrationTests.cs`

**Test**: `CreateOrder_TriggersWorkflow_CompletesOrExpiresOrder`
- Create order via API
- Wait for worker to process
- Verify order reached terminal state (completed or processing)
- Verify notifications created

---

## Documentation References

### External Documentation

**MassTransit**:
- Consumer Pattern: https://masstransit.io/documentation/concepts/consumers
- Publishing Events: https://masstransit.io/documentation/concepts/messages#publishing
- RabbitMQ Transport: https://masstransit.io/documentation/transports/rabbitmq
- Dependency Injection: https://masstransit.io/documentation/configuration/dependency-injection

**Entity Framework Core**:
- DbContext Lifetime: https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/#dbcontext-lifetime
- Querying Data: https://learn.microsoft.com/en-us/ef/core/querying/
- Change Tracking: https://learn.microsoft.com/en-us/ef/core/change-tracking/

**.NET Background Services**:
- BackgroundService: https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.backgroundservice
- IHostedService: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services
- PeriodicTimer: https://learn.microsoft.com/en-us/dotnet/api/system.threading.periodictimer

**Random Class**:
- Random.Shared: https://learn.microsoft.com/en-us/dotnet/api/system.random.shared
- Thread Safety: https://learn.microsoft.com/en-us/dotnet/api/system.random#thread-safety

### Internal Documentation

**Project Documentation**:
- Project Overview: `CLAUDE.md:5-13`
- Architecture Rules: `CLAUDE.md:68-104`
- Configuration Guidelines: `CLAUDE.md:106-127`
- Docker Setup: `CLAUDE.md:129-137`

**Configuration Files**:
- Instance Config: `Configuration\instance.json`
- Secrets Config: `Configuration\secrets.json`

**Existing Patterns**:
- Event Publishing: `AI.OrderProcessingSystem.WebApi\Controllers\OrdersController.cs:114-122`
- Scoped DbContext: `AI.OrderProcessingSystem.CronJob\Services\OrderExpiryService.cs:52-54`
- Notification Creation: `AI.OrderProcessingSystem.CronJob\Services\OrderExpiryService.cs:87-95`
- Consumer Registration: `AI.OrderProcessingSystem.Worker\Program.cs:39-70`

---

## Common Gotchas & Pitfalls

### 1. DbContext Lifetime in Consumers

**❌ Wrong**:
```csharp
public class MyConsumer : IConsumer<MyEvent>
{
    private readonly OrderProcessingDbContext _context; // Don't inject DbContext directly!

    public MyConsumer(OrderProcessingDbContext context) // This creates a singleton DbContext!
    {
        _context = context;
    }
}
```

**✅ Correct**:
```csharp
public class MyConsumer : IConsumer<MyEvent>
{
    private readonly IServiceProvider _serviceProvider;

    public MyConsumer(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task Consume(ConsumeContext<MyEvent> context)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderProcessingDbContext>();
        // Use dbContext here
    }
}
```

**Why?** MassTransit consumers are singletons, but DbContext must be scoped.

### 2. Event Publishing Order

**❌ Wrong**:
```csharp
await _eventPublisher.PublishAsync(completedEvent);
await context.SaveChangesAsync(); // Database save AFTER event publish
```

**✅ Correct**:
```csharp
await context.SaveChangesAsync(); // Database save FIRST
await _eventPublisher.PublishAsync(completedEvent); // Event publish AFTER
```

**Why?** If event publishing fails, you don't want inconsistent database state.

### 3. Random Number Generation Thread Safety

**❌ Wrong**:
```csharp
private readonly Random _random = new Random(); // Not thread-safe!

public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
{
    double value = _random.NextDouble(); // Can cause issues with concurrent consumers
}
```

**✅ Correct**:
```csharp
public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
{
    double value = Random.Shared.NextDouble(); // Thread-safe in .NET 6+
}
```

### 4. Configuration Not Registered

**Error**: `Unable to resolve service for type 'EventProcessingSettings'`

**Cause**: Forgot to register configuration in `Program.cs`

**Fix**: Add to Worker's `Program.cs`:
```csharp
var eventSettings = JsonSerializer.Deserialize<EventProcessingSettings>(
    instanceConfig!["EventProcessingSettings"].GetRawText())!;
builder.Services.AddSingleton(eventSettings);
```

### 5. MassTransitEventPublisher Not in Worker

**Error**: `Unable to resolve service for type 'IEventPublisher'`

**Cause**: `MassTransitEventPublisher` only exists in WebApi project

**Fix**: Either:
- Option A: Create `Worker\Services\MassTransitEventPublisher.cs` (copy from WebApi)
- Option B: Move to Common project (requires refactoring)

For this PRP, use **Option A** (simpler, no architectural changes).

### 6. Forgetting to Remove Consumer Registration

**Error**: `Type 'OrderExpiredConsumer' not found` when Worker starts

**Cause**: Consumer registration still in `Program.cs` after deleting consumer file

**Fix**: Remove from Worker's `Program.cs`:
- Line 44: `x.AddConsumer<OrderExpiredConsumer>();`
- Lines 65-68: Queue endpoint configuration

### 7. UTC vs Local DateTime

**❌ Wrong**:
```csharp
order.UpdatedAt = DateTime.Now; // Local time!
```

**✅ Correct**:
```csharp
order.UpdatedAt = DateTime.UtcNow; // Always use UTC
```

**Why?** Consistency across timezones. See existing pattern in `OrdersController.cs:104`.

### 8. Null Order Check

**❌ Wrong**:
```csharp
var order = await context.Orders.FirstOrDefaultAsync(o => o.Id == message.OrderId);
order.Status = "processing"; // NullReferenceException if order not found!
```

**✅ Correct**:
```csharp
var order = await context.Orders.FirstOrDefaultAsync(o => o.Id == message.OrderId);
if (order == null)
{
    _logger.LogWarning("Order {OrderId} not found, skipping processing", message.OrderId);
    return;
}
order.Status = "processing";
```

---

## Success Criteria

### Implementation Complete When:

✅ **All files modified/deleted as specified**
✅ **Solution builds without errors or warnings**
✅ **All existing tests pass**
✅ **Configuration values updated**
✅ **Manual workflow test succeeds**
✅ **Logs show expected messages**
✅ **Database state correct** (notifications saved, statuses updated)

### Behavioral Success Criteria:

1. ✅ Create order → status = "pending"
2. ✅ Wait 5 seconds → status = "processing"
3. ✅ After processing:
   - 50% chance: status = "completed", notification saved, fake email logged
   - 50% chance: status stays "processing", no notification
4. ✅ After 10 minutes in "processing" → status = "expired"
5. ✅ No `OrderExpiredEvent` published
6. ✅ Manual status updates don't trigger events

---

## PRP Quality Score: 9/10

### Why 9/10?

**Strengths**:
- ✅ Complete context from codebase
- ✅ Detailed implementation steps with code examples
- ✅ Clear validation gates
- ✅ Extensive gotchas section
- ✅ All patterns referenced with file paths and line numbers
- ✅ External documentation links provided
- ✅ Testing strategy included

**Minor Gaps** (-1 point):
- Integration test implementation not provided (optional)
- Could include more detailed error handling scenarios
- Could provide exact Consumer test code (currently just test cases)

**Confidence Level**: **High** - This PRP should enable one-pass implementation with minimal back-and-forth.

---

## Notes for Implementation

### Recommended Implementation Order

1. Start with **Phase 1** (cleanup) - safest changes
2. Test build after Phase 1
3. Move to **Phase 2 & 3** (CRON + Controller fixes)
4. Test build after Phase 3
5. Implement **Phase 4** (core consumer logic) - highest risk
6. **Phase 5** (Worker DI registration) - required for Phase 4 to work
7. Full integration test after all phases

### Time Estimates (for AI implementation)

- Phase 1: ~5 minutes
- Phase 2: ~3 minutes
- Phase 3: ~2 minutes
- Phase 4: ~15 minutes (core logic)
- Phase 5: ~10 minutes (DI setup)
- Testing: ~10 minutes
- **Total**: ~45 minutes for careful implementation

### Rollback Strategy

If implementation fails:
- Phases 1-3 are low risk, minimal rollback needed
- Phase 4-5 can be reverted by restoring consumer files
- Keep git commits small for easy rollback

### Architecture Compliance

All changes comply with project reference rules from `CLAUDE.md`:
- Worker references: Common ✓, Dal ✓
- No circular dependencies introduced ✓
- Configuration pattern consistent ✓

---

## Appendix: Complete Code Examples

### Example: OrderCreatedConsumer (Complete Implementation)

```csharp
using AI.OrderProcessingSystem.Common.Abstractions;
using AI.OrderProcessingSystem.Common.Configuration;
using AI.OrderProcessingSystem.Common.Events;
using AI.OrderProcessingSystem.Dal.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AI.OrderProcessingSystem.Worker.Consumers;

public class OrderCreatedConsumer : IConsumer<OrderCreatedEvent>
{
    private readonly ILogger<OrderCreatedConsumer> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly EventProcessingSettings _eventSettings;

    public OrderCreatedConsumer(
        ILogger<OrderCreatedConsumer> logger,
        IServiceProvider serviceProvider,
        EventProcessingSettings eventSettings)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _eventSettings = eventSettings;
    }

    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "Processing OrderCreatedEvent: OrderId={OrderId}, UserId={UserId}, Total={Total}",
            message.OrderId, message.UserId, message.Total);

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderProcessingDbContext>();
        var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

        // Load order
        var order = await dbContext.Orders
            .FirstOrDefaultAsync(o => o.Id == message.OrderId);

        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found, skipping processing", message.OrderId);
            return;
        }

        // Update status to processing
        order.Status = "processing";
        order.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        _logger.LogInformation("Order {OrderId} status changed to processing", order.Id);

        // Simulate payment processing
        await Task.Delay(TimeSpan.FromSeconds(_eventSettings.PaymentProcessingDelaySeconds));

        // Random success/failure
        double randomValue = Random.Shared.NextDouble();

        if (randomValue < _eventSettings.OrderCompletionSuccessRate)
        {
            // Success - complete order
            order.Status = "completed";
            order.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();

            _logger.LogInformation("Order {OrderId} completed successfully", order.Id);

            // Publish OrderCompletedEvent
            var completedEvent = new OrderCompletedEvent
            {
                OrderId = order.Id,
                UserId = order.UserId,
                Total = order.Total,
                CompletedAt = order.UpdatedAt
            };

            await eventPublisher.PublishAsync(completedEvent);
        }
        else
        {
            // Failure - leave in processing
            _logger.LogInformation(
                "Order {OrderId} payment failed (random={Random:F2}), remaining in processing",
                order.Id, randomValue);
        }
    }
}
```

### Example: OrderCompletedConsumer (Complete Implementation)

```csharp
using AI.OrderProcessingSystem.Common.Events;
using AI.OrderProcessingSystem.Dal.Data;
using AI.OrderProcessingSystem.Dal.Entities;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AI.OrderProcessingSystem.Worker.Consumers;

public class OrderCompletedConsumer : IConsumer<OrderCompletedEvent>
{
    private readonly ILogger<OrderCompletedConsumer> _logger;
    private readonly IServiceProvider _serviceProvider;

    public OrderCompletedConsumer(
        ILogger<OrderCompletedConsumer> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task Consume(ConsumeContext<OrderCompletedEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "Processing OrderCompletedEvent: OrderId={OrderId}, UserId={UserId}, Total={Total}",
            message.OrderId, message.UserId, message.Total);

        // Fake email sending
        _logger.LogInformation(
            "✉️ Sending email to user {UserId} for completed order {OrderId}",
            message.UserId, message.OrderId);

        await Task.Delay(500); // Simulate email sending

        _logger.LogInformation("✉️ Email sent successfully for order {OrderId}", message.OrderId);

        // Save notification to database
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderProcessingDbContext>();

        var notification = new Notification
        {
            OrderId = message.OrderId,
            EventType = "OrderCompleted",
            Message = $"Order #{message.OrderId} completed successfully",
            IsEmailSent = true,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Notifications.Add(notification);
        await dbContext.SaveChangesAsync();

        _logger.LogInformation("Notification saved for order {OrderId}", message.OrderId);
    }
}
```

### Example: Worker Program.cs (DI Registration Addition)

Add after line 32 (after RabbitMQ settings):

```csharp
// Build EventProcessingSettings
var eventProcessingSettings = JsonSerializer.Deserialize<EventProcessingSettings>(
    instanceConfig!["EventProcessingSettings"].GetRawText())!;

// Build connection string
var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                       ?? secretsConfig!["ConnectionStrings"].GetProperty("DefaultConnection").GetString()!;

// Validate configuration
if (string.IsNullOrEmpty(connectionString))
    throw new InvalidOperationException("Database connection string is not configured");

// Register configuration
builder.Services.AddSingleton(eventProcessingSettings);

// Add DbContext
builder.Services.AddDbContext<OrderProcessingDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add IEventPublisher
builder.Services.AddScoped<IEventPublisher, MassTransitEventPublisher>();
```

Then add the MassTransitEventPublisher class at the end of Program.cs (after line 74):

```csharp
// MassTransit Event Publisher
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

    public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            await _publishEndpoint.Publish(message, cancellationToken);
            _logger.LogInformation("Published event {EventType}: {@Event}", typeof(T).Name, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event {EventType}: {@Event}", typeof(T).Name, message);
            throw;
        }
    }
}
```

Don't forget to add the required using statements at the top:
```csharp
using AI.OrderProcessingSystem.Common.Abstractions;
using AI.OrderProcessingSystem.Dal.Data;
using AI.OrderProcessingSystem.Dal.Entities;
using Microsoft.EntityFrameworkCore;
```

---

**End of PRP**
