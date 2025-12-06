using AI.OrderProcessingSystem.Dal.Entities;
using Microsoft.EntityFrameworkCore;

namespace AI.OrderProcessingSystem.Dal.Data;

public static class DbInitializer
{
    public static void SeedData(ModelBuilder modelBuilder, string adminEmail, string adminPasswordHash)
    {
        // Seed Admin User
        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = 1,
                Name = "Administrator",
                Email = adminEmail,
                Password = adminPasswordHash
            }
        );

        // Seed sample products
        modelBuilder.Entity<Product>().HasData(
            new Product
            {
                Id = 1,
                Name = "Sample Product 1",
                Description = "This is a sample product for testing",
                Price = 99.99m,
                Stock = 100,
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Product
            {
                Id = 2,
                Name = "Sample Product 2",
                Description = "Another sample product",
                Price = 149.99m,
                Stock = 50,
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
