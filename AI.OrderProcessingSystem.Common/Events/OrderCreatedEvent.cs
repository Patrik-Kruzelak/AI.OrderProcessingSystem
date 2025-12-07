namespace AI.OrderProcessingSystem.Common.Events;

public record OrderCreatedEvent
{
    public int OrderId { get; init; }
    public int UserId { get; init; }
    public decimal Total { get; init; }
    public DateTime CreatedAt { get; init; }
}
