using System.ComponentModel.DataAnnotations;

namespace AI.OrderProcessingSystem.Common.DTOs.Products;

public class CreateProductDto
{
    [Required(ErrorMessage = "Product name is required")]
    [MaxLength(100, ErrorMessage = "Product name cannot exceed 100 characters")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Price is required")]
    [Range(0, double.MaxValue, ErrorMessage = "Price must be greater than or equal to 0")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Stock is required")]
    [Range(0, int.MaxValue, ErrorMessage = "Stock must be greater than or equal to 0")]
    public int Stock { get; set; }
}
