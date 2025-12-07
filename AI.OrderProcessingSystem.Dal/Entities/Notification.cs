using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AI.OrderProcessingSystem.Dal.Entities;

[Table("notifications")]
public class Notification
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("order_id")]
    public int OrderId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("event_type")]
    public string EventType { get; set; } = string.Empty;

    [Required]
    [Column("message")]
    public string Message { get; set; } = string.Empty;

    [Required]
    [Column("is_email_sent")]
    public bool IsEmailSent { get; set; }

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    // Navigation property
    [ForeignKey(nameof(OrderId))]
    public Order Order { get; set; } = null!;
}
