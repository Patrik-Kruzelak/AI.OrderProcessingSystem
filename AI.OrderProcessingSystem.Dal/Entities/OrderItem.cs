using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AI.OrderProcessingSystem.Dal.Entities;

[Table("order_items")]
public class OrderItem
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("order_id")]
    public int OrderId { get; set; }

    [Required]
    [Column("product_id")]
    public int ProductId { get; set; }

    [Required]
    [Column("quantity")]
    public int Quantity { get; set; }

    [Required]
    [Column("price")]
    public decimal Price { get; set; }

    // Navigation properties
    [ForeignKey(nameof(OrderId))]
    public Order Order { get; set; } = null!;

    [ForeignKey(nameof(ProductId))]
    public Product Product { get; set; } = null!;
}
