using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AI.OrderProcessingSystem.Dal.Entities;

[Table("products")]
public class Product
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Required]
    [Column("price")]
    public decimal Price { get; set; }

    [Required]
    [Column("stock")]
    public int Stock { get; set; }

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    // Navigation property
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
