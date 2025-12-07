namespace AI.OrderProcessingSystem.Common.Events;

public record OrderExpiredEvent
{
    public int OrderId { get; init; }
    public int UserId { get; init; }
    public decimal Total { get; init; }
    public DateTime ExpiredAt { get; init; }
    public int ExpiryThresholdMinutes { get; init; }
}
