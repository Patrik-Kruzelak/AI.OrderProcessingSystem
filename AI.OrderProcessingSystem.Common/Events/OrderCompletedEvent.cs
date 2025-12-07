namespace AI.OrderProcessingSystem.Common.Events;

public record OrderCompletedEvent
{
    public int OrderId { get; init; }
    public int UserId { get; init; }
    public decimal Total { get; init; }
    public DateTime CompletedAt { get; init; }
}
