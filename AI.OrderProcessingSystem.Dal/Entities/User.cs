using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AI.OrderProcessingSystem.Dal.Entities;

[Table("users")]
public class User
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Column("password")]
    public string Password { get; set; } = string.Empty;

    // Navigation property
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
