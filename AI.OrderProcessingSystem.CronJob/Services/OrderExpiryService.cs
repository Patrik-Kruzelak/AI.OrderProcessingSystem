using AI.OrderProcessingSystem.Common.Abstractions;
using AI.OrderProcessingSystem.Common.Configuration;
using AI.OrderProcessingSystem.Common.Events;
using AI.OrderProcessingSystem.Dal.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AI.OrderProcessingSystem.CronJob.Services;

public class OrderExpiryService : BackgroundService
{
    private readonly ILogger<OrderExpiryService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly EventProcessingSettings _eventSettings;
    private readonly int _checkIntervalSeconds;

    public OrderExpiryService(
        ILogger<OrderExpiryService> logger,
        IServiceProvider serviceProvider,
        EventProcessingSettings eventSettings)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _eventSettings = eventSettings;
        _checkIntervalSeconds = eventSettings.ExpiryCheckIntervalSeconds;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OrderExpiryService started. Checking every {Interval} seconds", _checkIntervalSeconds);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_checkIntervalSeconds));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await CheckAndExpireOrdersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking for expired orders");
            }
        }
    }

    private async Task CheckAndExpireOrdersAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrderProcessingDbContext>();
        var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

        var expiryThreshold = DateTime.UtcNow.AddMinutes(-_eventSettings.OrderExpiryThresholdMinutes);

        var expiredOrders = await context.Orders
            .Where(o => (o.Status == "processing" || o.Status == "pending") && o.CreatedAt < expiryThreshold)
            .ToListAsync(cancellationToken);

        if (expiredOrders.Count == 0)
        {
            _logger.LogInformation("No expired orders found. Checked for orders older than {Threshold} minutes", _eventSettings.OrderExpiryThresholdMinutes);
            return;
        }

        _logger.LogInformation("Found {Count} expired orders", expiredOrders.Count);

        foreach (var order in expiredOrders)
        {
            // Update order status to expired
            order.Status = "expired";
            order.UpdatedAt = DateTime.UtcNow;

            _logger.LogInformation("Order {OrderId} marked as expired", order.Id);
        }

        await context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully updated {Count} expired orders in database", expiredOrders.Count);

        // Publish OrderExpiredEvent for each expired order
        foreach (var order in expiredOrders)
        {
            var expiredEvent = new OrderExpiredEvent
            {
                OrderId = order.Id,
                UserId = order.UserId,
                Total = order.Total,
                ExpiredAt = order.UpdatedAt,
                ExpiryThresholdMinutes = _eventSettings.OrderExpiryThresholdMinutes
            };

            await eventPublisher.PublishAsync(expiredEvent);

            _logger.LogInformation("Published OrderExpiredEvent for order {OrderId}", order.Id);
        }

        _logger.LogInformation("Successfully published {Count} OrderExpiredEvents", expiredOrders.Count);
    }
}
