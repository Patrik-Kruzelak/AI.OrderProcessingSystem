using AI.OrderProcessingSystem.Dal.Entities;
using Microsoft.EntityFrameworkCore;

namespace AI.OrderProcessingSystem.Dal.Data;

public class OrderProcessingDbContext : DbContext
{
    public OrderProcessingDbContext(DbContextOptions<OrderProcessingDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Password).IsRequired();
        });

        // Product configuration
        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.Property(e => e.Stock).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Check constraints
            entity.HasCheckConstraint("CK_Product_Price", "price >= 0");
            entity.HasCheckConstraint("CK_Product_Stock", "stock >= 0");
        });

        // Order configuration
        modelBuilder.Entity<Order>(entity =>
        {
            entity.Property(e => e.Total).HasPrecision(18, 2);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Foreign key
            entity.HasOne(e => e.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Check constraints
            entity.HasCheckConstraint("CK_Order_Total", "total >= 0");
            entity.HasCheckConstraint("CK_Order_Status",
                "status IN ('pending', 'processing', 'completed', 'expired')");
        });

        // OrderItem configuration
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.Property(e => e.Quantity).IsRequired();
            entity.Property(e => e.Price).HasPrecision(18, 2);

            // Foreign keys
            entity.HasOne(e => e.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                .WithMany(p => p.OrderItems)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Check constraints
            entity.HasCheckConstraint("CK_OrderItem_Quantity", "quantity > 0");
            entity.HasCheckConstraint("CK_OrderItem_Price", "price > 0");
        });

        // Seed data - hash generated for password "Admin@12345"
        string adminPasswordHash = "$2a$11$LJZwXvKGWW7H5d5QfKvV8.Tw5aT9pR7o9vNK9DK/7nXHxF8yw2Dsy";
        DbInitializer.SeedData(modelBuilder, "admin@orderprocessing.local", adminPasswordHash);
    }
}
