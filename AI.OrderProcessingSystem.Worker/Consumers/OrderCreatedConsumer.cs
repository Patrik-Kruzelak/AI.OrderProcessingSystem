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
