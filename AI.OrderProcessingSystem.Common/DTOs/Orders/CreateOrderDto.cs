using System.ComponentModel.DataAnnotations;

namespace AI.OrderProcessingSystem.Common.DTOs.Orders;

public class CreateOrderDto
{
    [Required(ErrorMessage = "User ID is required")]
    public int UserId { get; set; }

    [Required(ErrorMessage = "Order items are required")]
    [MinLength(1, ErrorMessage = "Order must contain at least one item")]
    public List<CreateOrderItemDto> Items { get; set; } = new();
}

public class CreateOrderItemDto
{
    [Required(ErrorMessage = "Product ID is required")]
    public int ProductId { get; set; }

    [Required(ErrorMessage = "Quantity is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
    public int Quantity { get; set; }
}
