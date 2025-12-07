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
