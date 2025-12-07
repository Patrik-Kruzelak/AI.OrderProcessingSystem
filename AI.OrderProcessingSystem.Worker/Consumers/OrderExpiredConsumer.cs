using AI.OrderProcessingSystem.Common.Events;
using AI.OrderProcessingSystem.Dal.Data;
using AI.OrderProcessingSystem.Dal.Entities;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AI.OrderProcessingSystem.Worker.Consumers;

public class OrderExpiredConsumer : IConsumer<OrderExpiredEvent>
{
    private readonly ILogger<OrderExpiredConsumer> _logger;
    private readonly IServiceProvider _serviceProvider;

    public OrderExpiredConsumer(
        ILogger<OrderExpiredConsumer> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task Consume(ConsumeContext<OrderExpiredEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "Processing OrderExpiredEvent: OrderId={OrderId}, UserId={UserId}, Total={Total}",
            message.OrderId, message.UserId, message.Total);

        // Save notification to database (no email sent for expired orders)
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderProcessingDbContext>();

        var notification = new Notification
        {
            OrderId = message.OrderId,
            EventType = "OrderExpired",
            Message = $"Order #{message.OrderId} expired after {message.ExpiryThresholdMinutes} minutes",
            IsEmailSent = false,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Notifications.Add(notification);
        await dbContext.SaveChangesAsync();

        _logger.LogInformation("Notification saved for expired order {OrderId}", message.OrderId);
    }
}
