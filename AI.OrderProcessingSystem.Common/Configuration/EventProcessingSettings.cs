namespace AI.OrderProcessingSystem.Common.Configuration;

public class EventProcessingSettings
{
    public int PaymentProcessingDelaySeconds { get; set; }
    public double OrderCompletionSuccessRate { get; set; }
    public int OrderExpiryThresholdMinutes { get; set; }
    public int ExpiryCheckIntervalSeconds { get; set; }
}
