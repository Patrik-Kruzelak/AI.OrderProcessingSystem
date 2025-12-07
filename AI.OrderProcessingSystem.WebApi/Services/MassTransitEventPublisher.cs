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
