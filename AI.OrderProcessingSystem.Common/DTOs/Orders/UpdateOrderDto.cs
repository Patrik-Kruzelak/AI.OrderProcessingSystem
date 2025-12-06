using System.ComponentModel.DataAnnotations;

namespace AI.OrderProcessingSystem.Common.DTOs.Orders;

public class UpdateOrderDto
{
    [Required(ErrorMessage = "Status is required")]
    [MaxLength(20, ErrorMessage = "Status cannot exceed 20 characters")]
    public string Status { get; set; } = string.Empty;
}
