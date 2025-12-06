# PRP: Order Processing System API with PostgreSQL, JWT Authentication & Docker

## Executive Summary

Implement a complete REST API system for order processing with the following deliverables:

- PostgreSQL database running in Docker with Entity Framework Core
- Database models: User, Product, Order (with order items as nested schema)
- JWT-based authentication system
- CRUD REST APIs for User, Product, and Order entities
- Input validation with automatic 400 error responses
- Swagger/OpenAPI documentation with JWT bearer support
- Integration tests (minimum 5 test cases)
- Docker configuration for both database and API
- Database migration tooling and seed data
- Complete documentation in README.md

**Estimated Scope**: ~1,200 lines of code across 30+ files

---

## Context & Prerequisites

### Current Project State

The solution is organized as a multi-tier .NET 8 application with 5 projects:

**Existing Setup (AI.OrderProcessingSystem.WebApi)**:
- Basic ASP.NET Core MVC application
- Current entry point: `AI.OrderProcessingSystem.WebApi/Program.cs:1`
- Existing middleware pipeline: `AI.OrderProcessingSystem.WebApi/Program.cs:8-25`
- Sample controller pattern: `AI.OrderProcessingSystem.WebApi/Controllers/HomeController.cs:7`
- No NuGet packages yet beyond framework defaults
- Currently configured for MVC views, needs API controllers added

**Projects Requiring Conversion**:
- `AI.OrderProcessingSystem.Dal/AI.OrderProcessingSystem.Dal.csproj:4` - Console app (OutputType: Exe) → Class library
- `AI.OrderProcessingSystem.Common/AI.OrderProcessingSystem.Common.csproj:4` - Console app (OutputType: Exe) → Class library

**Missing Infrastructure**:
- No `Configuration` directory (needs creation at repository root)
- No `Dockerfile` (must be at repository root per CLAUDE.md:129)
- No `docker-compose.yml` (must be at repository root per CLAUDE.md:129)
- No test projects (needs creation)
- No README.md file

### Architecture Constraints (from CLAUDE.md)

**Project Reference Rules**:
- **AI.OrderProcessingSystem.WebApi** MAY reference:
  - AI.OrderProcessingSystem.Common ✓
  - AI.OrderProcessingSystem.Dal ✓
  - MUST NOT reference: Worker, CronJob ✗

- **AI.OrderProcessingSystem.Dal** MAY reference:
  - AI.OrderProcessingSystem.Common ✓
  - MUST NOT reference: WebApi, Worker, CronJob ✗

- **AI.OrderProcessingSystem.Common**:
  - MUST NOT reference any other project (per CLAUDE.md:86)

**Configuration Requirements (CLAUDE.md:113-127)**:
1. Load `\Configuration\secrets.json` (DB credentials, JWT secret, etc.)
2. Load `\Configuration\instance.json` (non-sensitive URLs, settings)
3. Merge into single runtime configuration object
4. Validate both files at application startup

**Docker Requirements (CLAUDE.md:129-136)**:
- Dockerfile must be at repository root `\`
- docker-compose.yml must be at repository root `\`
- Must reference configuration files for environment loading

---

## Technical Stack & Dependencies

### NuGet Packages Required

**AI.OrderProcessingSystem.Dal**:
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.11" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.11">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.10" />
```

**AI.OrderProcessingSystem.WebApi**:
```xml
<!-- EF Core Tools for migrations -->
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.11">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>

<!-- JWT Authentication -->
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.11" />
<PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.2.1" />

<!-- Password Hashing -->
<PackageReference Include="BCrypt.Net-Next" Version="4.0.3" />

<!-- Swagger/OpenAPI -->
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.9.0" />

<!-- For ProblemDetails support -->
<PackageReference Include="Hellang.Middleware.ProblemDetails" Version="6.5.1" />
```

**AI.OrderProcessingSystem.WebApi.Tests** (new project):
```xml
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
<PackageReference Include="xunit" Version="2.9.2" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.11" />
<PackageReference Include="Testcontainers.PostgreSql" Version="3.10.0" />
```

### Why These Packages?

- **EF Core + Npgsql**: Entity Framework with PostgreSQL provider
- **JWT packages**: Standard Microsoft JWT implementation for ASP.NET Core 8
- **BCrypt.Net-Next**: Industry-standard password hashing (better than plain PBKDF2)
- **Swashbuckle**: De facto standard for Swagger in ASP.NET Core
- **Hellang.Middleware.ProblemDetails**: RFC 7807 compliant error responses
- **Testcontainers.PostgreSql**: Run real PostgreSQL in Docker for integration tests

---

## Implementation Blueprint

### Phase 1: Project Structure Setup

#### 1.1 Convert Console Apps to Class Libraries

**AI.OrderProcessingSystem.Dal/AI.OrderProcessingSystem.Dal.csproj**:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <!-- REMOVE THIS LINE: <OutputType>Exe</OutputType> -->
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.11" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.11">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.10" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\AI.OrderProcessingSystem.Common\AI.OrderProcessingSystem.Common.csproj" />
  </ItemGroup>
</Project>
```

**AI.OrderProcessingSystem.Common/AI.OrderProcessingSystem.Common.csproj**:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <!-- REMOVE THIS LINE: <OutputType>Exe</OutputType> -->
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <!-- Add validation annotations package -->
  <ItemGroup>
    <PackageReference Include="System.ComponentModel.Annotations" Version="5.0.0" />
  </ItemGroup>
</Project>
```

**Actions**:
- Delete `AI.OrderProcessingSystem.Dal/Program.cs`
- Delete `AI.OrderProcessingSystem.Common/Program.cs`
- Update both .csproj files as shown above
- Run `dotnet restore AI.OrderProcessingSystem.sln` to verify

#### 1.2 Add Project References to WebApi

**AI.OrderProcessingSystem.WebApi/AI.OrderProcessingSystem.WebApi.csproj**:
```xml
<ItemGroup>
  <ProjectReference Include="..\AI.OrderProcessingSystem.Common\AI.OrderProcessingSystem.Common.csproj" />
  <ProjectReference Include="..\AI.OrderProcessingSystem.Dal\AI.OrderProcessingSystem.Dal.csproj" />
</ItemGroup>
```

#### 1.3 Create Configuration Directory and Files

**\Configuration\secrets.json**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=orderprocessing;Username=postgres;Password=SecureP@ssw0rd123"
  },
  "JwtSettings": {
    "SecretKey": "your-super-secure-256-bit-secret-key-minimum-32-characters-required-for-hs256",
    "Issuer": "AI.OrderProcessingSystem",
    "Audience": "AI.OrderProcessingSystem.WebApi",
    "ExpirationMinutes": 60
  },
  "AdminUser": {
    "Email": "admin@orderprocessing.local",
    "Password": "Admin@12345"
  }
}
```

**\Configuration\instance.json**:
```json
{
  "AppSettings": {
    "ApiTitle": "Order Processing System API",
    "ApiVersion": "v1",
    "ApiDescription": "RESTful API for order processing with JWT authentication",
    "AllowedOrigins": [
      "http://localhost:3000",
      "https://localhost:3000"
    ]
  },
  "Database": {
    "CommandTimeout": 30,
    "MaxRetryCount": 3,
    "EnableSensitiveDataLogging": false
  }
}
```

**IMPORTANT**: Add to `.gitignore` if secrets.json shouldn't be committed (though per CLAUDE.md:118, it should be in Git for this project)

---

### Phase 2: Database Layer (AI.OrderProcessingSystem.Dal)

#### 2.1 Entity Models

**Directory Structure**:
```
AI.OrderProcessingSystem.Dal/
  Entities/
    User.cs
    Product.cs
    Order.cs
    OrderItem.cs
  Data/
    OrderProcessingDbContext.cs
    DbInitializer.cs
  Migrations/
    (generated by EF Core)
```

**AI.OrderProcessingSystem.Dal/Entities/User.cs**:
```csharp
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
```

**AI.OrderProcessingSystem.Dal/Entities/Product.cs**:
```csharp
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
```

**AI.OrderProcessingSystem.Dal/Entities/Order.cs**:
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AI.OrderProcessingSystem.Dal.Entities;

[Table("orders")]
public class Order
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("user_id")]
    public int UserId { get; set; }

    [Required]
    [Column("total")]
    public decimal Total { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = "pending";

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Required]
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
```

**AI.OrderProcessingSystem.Dal/Entities/OrderItem.cs**:
```csharp
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
```

#### 2.2 DbContext Configuration

**AI.OrderProcessingSystem.Dal/Data/OrderProcessingDbContext.cs**:
```csharp
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

        // Seed data will be added here (see Phase 2.3)
    }
}
```

#### 2.3 Seed Data for Admin User

**AI.OrderProcessingSystem.Dal/Data/DbInitializer.cs**:
```csharp
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
                CreatedAt = DateTime.UtcNow
            },
            new Product
            {
                Id = 2,
                Name = "Sample Product 2",
                Description = "Another sample product",
                Price = 149.99m,
                Stock = 50,
                CreatedAt = DateTime.UtcNow
            }
        );
    }
}
```

**Update OnModelCreating in OrderProcessingDbContext.cs**:
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // ... existing configuration ...

    // Seed data
    // Note: Password hash will be generated during migration
    // For now, use a placeholder. The actual hash should be BCrypt hashed
    string adminPasswordHash = "$2a$11$placeholder"; // Will be replaced with actual BCrypt hash
    DbInitializer.SeedData(modelBuilder, "admin@orderprocessing.local", adminPasswordHash);
}
```

**CRITICAL**: The admin password hash must be generated using BCrypt during the migration creation. The actual password will be provided in secrets.json, and it should be hashed BEFORE adding to seed data.

---

### Phase 3: Shared Layer (AI.OrderProcessingSystem.Common)

#### 3.1 DTOs Directory Structure

```
AI.OrderProcessingSystem.Common/
  DTOs/
    Auth/
      LoginRequestDto.cs
      LoginResponseDto.cs
    Users/
      CreateUserDto.cs
      UpdateUserDto.cs
      UserResponseDto.cs
    Products/
      CreateProductDto.cs
      UpdateProductDto.cs
      ProductResponseDto.cs
    Orders/
      CreateOrderDto.cs
      CreateOrderItemDto.cs
      UpdateOrderDto.cs
      OrderResponseDto.cs
      OrderItemResponseDto.cs
  Enums/
    OrderStatus.cs
  Constants/
    ValidationConstants.cs
```

#### 3.2 Sample DTOs

**AI.OrderProcessingSystem.Common/DTOs/Auth/LoginRequestDto.cs**:
```csharp
using System.ComponentModel.DataAnnotations;

namespace AI.OrderProcessingSystem.Common.DTOs.Auth;

public class LoginRequestDto
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    public string Password { get; set; } = string.Empty;
}
```

**AI.OrderProcessingSystem.Common/DTOs/Auth/LoginResponseDto.cs**:
```csharp
namespace AI.OrderProcessingSystem.Common.DTOs.Auth;

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserInfo User { get; set; } = null!;

    public class UserInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
```

**AI.OrderProcessingSystem.Common/DTOs/Users/CreateUserDto.cs**:
```csharp
using System.ComponentModel.DataAnnotations;

namespace AI.OrderProcessingSystem.Common.DTOs.Users;

public class CreateUserDto
{
    [Required(ErrorMessage = "Name is required")]
    [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [MaxLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    public string Password { get; set; } = string.Empty;
}
```

**AI.OrderProcessingSystem.Common/DTOs/Products/CreateProductDto.cs**:
```csharp
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
```

**AI.OrderProcessingSystem.Common/DTOs/Orders/CreateOrderDto.cs**:
```csharp
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
```

**AI.OrderProcessingSystem.Common/Enums/OrderStatus.cs**:
```csharp
namespace AI.OrderProcessingSystem.Common.Enums;

public enum OrderStatus
{
    Pending,
    Processing,
    Completed,
    Expired
}
```

---

### Phase 4: Authentication Infrastructure (AI.OrderProcessingSystem.WebApi)

#### 4.1 Configuration Models

**AI.OrderProcessingSystem.WebApi/Configuration/JwtSettings.cs**:
```csharp
namespace AI.OrderProcessingSystem.WebApi.Configuration;

public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; }
}
```

**AI.OrderProcessingSystem.WebApi/Configuration/AppConfiguration.cs**:
```csharp
namespace AI.OrderProcessingSystem.WebApi.Configuration;

public class AppConfiguration
{
    public string ConnectionString { get; set; } = string.Empty;
    public JwtSettings JwtSettings { get; set; } = new();
    public AdminUserConfig AdminUser { get; set; } = new();
    public AppSettings AppSettings { get; set; } = new();
}

public class AdminUserConfig
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AppSettings
{
    public string ApiTitle { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = string.Empty;
    public string ApiDescription { get; set; } = string.Empty;
}
```

#### 4.2 JWT Token Service

**AI.OrderProcessingSystem.WebApi/Services/IJwtTokenService.cs**:
```csharp
using AI.OrderProcessingSystem.Dal.Entities;

namespace AI.OrderProcessingSystem.WebApi.Services;

public interface IJwtTokenService
{
    string GenerateToken(User user);
    DateTime GetTokenExpiration();
}
```

**AI.OrderProcessingSystem.WebApi/Services/JwtTokenService.cs**:
```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AI.OrderProcessingSystem.Dal.Entities;
using AI.OrderProcessingSystem.WebApi.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AI.OrderProcessingSystem.WebApi.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _jwtSettings;

    public JwtTokenService(JwtSettings jwtSettings)
    {
        _jwtSettings = jwtSettings;
    }

    public string GenerateToken(User user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name, user.Name),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public DateTime GetTokenExpiration()
    {
        return DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes);
    }
}
```

#### 4.3 Password Service

**AI.OrderProcessingSystem.WebApi/Services/IPasswordService.cs**:
```csharp
namespace AI.OrderProcessingSystem.WebApi.Services;

public interface IPasswordService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}
```

**AI.OrderProcessingSystem.WebApi/Services/PasswordService.cs**:
```csharp
namespace AI.OrderProcessingSystem.WebApi.Services;

public class PasswordService : IPasswordService
{
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
```

---

### Phase 5: API Layer (AI.OrderProcessingSystem.WebApi)

#### 5.1 Updated Program.cs

**AI.OrderProcessingSystem.WebApi/Program.cs** (complete replacement):
```csharp
using System.Text;
using System.Text.Json;
using AI.OrderProcessingSystem.Dal.Data;
using AI.OrderProcessingSystem.WebApi.Configuration;
using AI.OrderProcessingSystem.WebApi.Services;
using Hellang.Middleware.ProblemDetails;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Load configuration files
var secretsPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Configuration", "secrets.json");
var instancePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Configuration", "instance.json");

if (!File.Exists(secretsPath))
    throw new FileNotFoundException($"Configuration file not found: {secretsPath}");
if (!File.Exists(instancePath))
    throw new FileNotFoundException($"Configuration file not found: {instancePath}");

var secretsConfig = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
    File.ReadAllText(secretsPath), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
var instanceConfig = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
    File.ReadAllText(instancePath), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

// Build merged configuration
var appConfig = new AppConfiguration
{
    ConnectionString = secretsConfig!["ConnectionStrings"].GetProperty("DefaultConnection").GetString()!,
    JwtSettings = JsonSerializer.Deserialize<JwtSettings>(secretsConfig["JwtSettings"].GetRawText())!,
    AdminUser = JsonSerializer.Deserialize<AdminUserConfig>(secretsConfig["AdminUser"].GetRawText())!,
    AppSettings = JsonSerializer.Deserialize<AppSettings>(instanceConfig!["AppSettings"].GetRawText())!
};

// Validate configuration
if (string.IsNullOrEmpty(appConfig.ConnectionString))
    throw new InvalidOperationException("Database connection string is not configured");
if (string.IsNullOrEmpty(appConfig.JwtSettings.SecretKey) || appConfig.JwtSettings.SecretKey.Length < 32)
    throw new InvalidOperationException("JWT SecretKey must be at least 32 characters");

// Register configuration
builder.Services.AddSingleton(appConfig.JwtSettings);
builder.Services.AddSingleton(appConfig);

// Add DbContext
builder.Services.AddDbContext<OrderProcessingDbContext>(options =>
    options.UseNpgsql(appConfig.ConnectionString));

// Add services
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();

// Add controllers
builder.Services.AddControllers();

// Add ProblemDetails middleware
builder.Services.AddProblemDetails(options =>
{
    options.IncludeExceptionDetails = (ctx, ex) => builder.Environment.IsDevelopment();
    options.MapToStatusCode<NotImplementedException>(StatusCodes.Status501NotImplemented);
    options.MapToStatusCode<UnauthorizedAccessException>(StatusCodes.Status401Unauthorized);
});

// Add JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = appConfig.JwtSettings.Issuer,
            ValidAudience = appConfig.JwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(appConfig.JwtSettings.SecretKey))
        };
    });

builder.Services.AddAuthorization();

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = appConfig.AppSettings.ApiTitle,
        Version = appConfig.AppSettings.ApiVersion,
        Description = appConfig.AppSettings.ApiDescription
    });

    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Use ProblemDetails middleware
app.UseProblemDetails();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", $"{appConfig.AppSettings.ApiTitle} {appConfig.AppSettings.ApiVersion}");
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Run migrations and seed data on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OrderProcessingDbContext>();
    dbContext.Database.Migrate();
}

app.Run();

// Make Program class public for testing
public partial class Program { }
```

#### 5.2 AuthController

**AI.OrderProcessingSystem.WebApi/Controllers/AuthController.cs**:
```csharp
using AI.OrderProcessingSystem.Common.DTOs.Auth;
using AI.OrderProcessingSystem.Dal.Data;
using AI.OrderProcessingSystem.WebApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AI.OrderProcessingSystem.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly OrderProcessingDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPasswordService _passwordService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        OrderProcessingDbContext context,
        IJwtTokenService jwtTokenService,
        IPasswordService passwordService,
        ILogger<AuthController> logger)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _passwordService = passwordService;
        _logger = logger;
    }

    /// <summary>
    /// Authenticates a user and returns a JWT token
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>JWT token and user information</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null)
        {
            _logger.LogWarning("Login attempt failed: User not found for email {Email}", request.Email);
            return Unauthorized(new { message = "Invalid email or password" });
        }

        if (!_passwordService.VerifyPassword(request.Password, user.Password))
        {
            _logger.LogWarning("Login attempt failed: Invalid password for email {Email}", request.Email);
            return Unauthorized(new { message = "Invalid email or password" });
        }

        var token = _jwtTokenService.GenerateToken(user);
        var expiresAt = _jwtTokenService.GetTokenExpiration();

        _logger.LogInformation("User {UserId} logged in successfully", user.Id);

        return Ok(new LoginResponseDto
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = new LoginResponseDto.UserInfo
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email
            }
        });
    }
}
```

#### 5.3 UsersController

**AI.OrderProcessingSystem.WebApi/Controllers/UsersController.cs**:
```csharp
using AI.OrderProcessingSystem.Common.DTOs.Users;
using AI.OrderProcessingSystem.Dal.Data;
using AI.OrderProcessingSystem.Dal.Entities;
using AI.OrderProcessingSystem.WebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AI.OrderProcessingSystem.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly OrderProcessingDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        OrderProcessingDbContext context,
        IPasswordService passwordService,
        ILogger<UsersController> logger)
    {
        _context = context;
        _passwordService = passwordService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<UserResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<UserResponseDto>>> GetAll()
    {
        var users = await _context.Users
            .Select(u => new UserResponseDto
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponseDto>> GetById(int id)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
            return NotFound(new { message = $"User with ID {id} not found" });

        return Ok(new UserResponseDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email
        });
    }

    [HttpPost]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserResponseDto>> Create([FromBody] CreateUserDto dto)
    {
        // Check if email already exists
        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            return BadRequest(new { message = "Email already exists" });

        var user = new User
        {
            Name = dto.Name,
            Email = dto.Email,
            Password = _passwordService.HashPassword(dto.Password)
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User created with ID {UserId}", user.Id);

        var response = new UserResponseDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email
        };

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, response);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponseDto>> Update(int id, [FromBody] UpdateUserDto dto)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
            return NotFound(new { message = $"User with ID {id} not found" });

        // Check if email is being changed and already exists
        if (dto.Email != user.Email && await _context.Users.AnyAsync(u => u.Email == dto.Email))
            return BadRequest(new { message = "Email already exists" });

        user.Name = dto.Name;
        user.Email = dto.Email;

        if (!string.IsNullOrEmpty(dto.Password))
            user.Password = _passwordService.HashPassword(dto.Password);

        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} updated", user.Id);

        return Ok(new UserResponseDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email
        });
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
            return NotFound(new { message = $"User with ID {id} not found" });

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} deleted", user.Id);

        return NoContent();
    }
}
```

#### 5.4 ProductsController

**AI.OrderProcessingSystem.WebApi/Controllers/ProductsController.cs**:
```csharp
using AI.OrderProcessingSystem.Common.DTOs.Products;
using AI.OrderProcessingSystem.Dal.Data;
using AI.OrderProcessingSystem.Dal.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AI.OrderProcessingSystem.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly OrderProcessingDbContext _context;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(OrderProcessingDbContext context, ILogger<ProductsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<ProductResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ProductResponseDto>>> GetAll()
    {
        var products = await _context.Products
            .Select(p => new ProductResponseDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                Stock = p.Stock,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();

        return Ok(products);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ProductResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponseDto>> GetById(int id)
    {
        var product = await _context.Products.FindAsync(id);

        if (product == null)
            return NotFound(new { message = $"Product with ID {id} not found" });

        return Ok(new ProductResponseDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            Stock = product.Stock,
            CreatedAt = product.CreatedAt
        });
    }

    [HttpPost]
    [ProducesResponseType(typeof(ProductResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductResponseDto>> Create([FromBody] CreateProductDto dto)
    {
        var product = new Product
        {
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            Stock = dto.Stock,
            CreatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Product created with ID {ProductId}", product.Id);

        var response = new ProductResponseDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            Stock = product.Stock,
            CreatedAt = product.CreatedAt
        };

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, response);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ProductResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponseDto>> Update(int id, [FromBody] UpdateProductDto dto)
    {
        var product = await _context.Products.FindAsync(id);

        if (product == null)
            return NotFound(new { message = $"Product with ID {id} not found" });

        product.Name = dto.Name;
        product.Description = dto.Description;
        product.Price = dto.Price;
        product.Stock = dto.Stock;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Product {ProductId} updated", product.Id);

        return Ok(new ProductResponseDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            Stock = product.Stock,
            CreatedAt = product.CreatedAt
        });
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _context.Products.FindAsync(id);

        if (product == null)
            return NotFound(new { message = $"Product with ID {id} not found" });

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Product {ProductId} deleted", product.Id);

        return NoContent();
    }
}
```

#### 5.5 OrdersController

**AI.OrderProcessingSystem.WebApi/Controllers/OrdersController.cs**:
```csharp
using AI.OrderProcessingSystem.Common.DTOs.Orders;
using AI.OrderProcessingSystem.Dal.Data;
using AI.OrderProcessingSystem.Dal.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AI.OrderProcessingSystem.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly OrderProcessingDbContext _context;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(OrderProcessingDbContext context, ILogger<OrdersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<OrderResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<OrderResponseDto>>> GetAll()
    {
        var orders = await _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .Include(o => o.User)
            .Select(o => MapToResponseDto(o))
            .ToListAsync();

        return Ok(orders);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponseDto>> GetById(int id)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound(new { message = $"Order with ID {id} not found" });

        return Ok(MapToResponseDto(order));
    }

    [HttpPost]
    [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderResponseDto>> Create([FromBody] CreateOrderDto dto)
    {
        // Validate user exists
        var user = await _context.Users.FindAsync(dto.UserId);
        if (user == null)
            return BadRequest(new { message = $"User with ID {dto.UserId} not found" });

        // Validate products and calculate total
        decimal total = 0;
        var orderItems = new List<OrderItem>();

        foreach (var itemDto in dto.Items)
        {
            var product = await _context.Products.FindAsync(itemDto.ProductId);
            if (product == null)
                return BadRequest(new { message = $"Product with ID {itemDto.ProductId} not found" });

            if (product.Stock < itemDto.Quantity)
                return BadRequest(new { message = $"Insufficient stock for product {product.Name}" });

            var itemPrice = product.Price;
            total += itemPrice * itemDto.Quantity;

            orderItems.Add(new OrderItem
            {
                ProductId = itemDto.ProductId,
                Quantity = itemDto.Quantity,
                Price = itemPrice
            });

            // Decrease stock
            product.Stock -= itemDto.Quantity;
        }

        var order = new Order
        {
            UserId = dto.UserId,
            Total = total,
            Status = "pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Items = orderItems
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Order created with ID {OrderId}", order.Id);

        // Reload with navigation properties
        await _context.Entry(order).Reference(o => o.User).LoadAsync();
        await _context.Entry(order).Collection(o => o.Items).LoadAsync();
        foreach (var item in order.Items)
        {
            await _context.Entry(item).Reference(i => i.Product).LoadAsync();
        }

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, MapToResponseDto(order));
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponseDto>> Update(int id, [FromBody] UpdateOrderDto dto)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound(new { message = $"Order with ID {id} not found" });

        // Validate status
        var validStatuses = new[] { "pending", "processing", "completed", "expired" };
        if (!validStatuses.Contains(dto.Status.ToLower()))
            return BadRequest(new { message = "Invalid order status" });

        order.Status = dto.Status.ToLower();
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Order {OrderId} updated", order.Id);

        return Ok(MapToResponseDto(order));
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound(new { message = $"Order with ID {id} not found" });

        // Restore stock for order items
        foreach (var item in order.Items)
        {
            var product = await _context.Products.FindAsync(item.ProductId);
            if (product != null)
            {
                product.Stock += item.Quantity;
            }
        }

        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Order {OrderId} deleted", order.Id);

        return NoContent();
    }

    private static OrderResponseDto MapToResponseDto(Order order)
    {
        return new OrderResponseDto
        {
            Id = order.Id,
            UserId = order.UserId,
            UserName = order.User?.Name ?? string.Empty,
            Total = order.Total,
            Status = order.Status,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            Items = order.Items.Select(i => new OrderItemResponseDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.Product?.Name ?? string.Empty,
                Quantity = i.Quantity,
                Price = i.Price
            }).ToList()
        };
    }
}
```

**Note**: Complete all Response DTOs (UserResponseDto, ProductResponseDto, OrderResponseDto, etc.) and Update DTOs in Phase 3.

---

### Phase 6: Docker Infrastructure

#### 6.1 Dockerfile

**\Dockerfile**:
```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY AI.OrderProcessingSystem.sln .
COPY AI.OrderProcessingSystem.WebApi/AI.OrderProcessingSystem.WebApi.csproj AI.OrderProcessingSystem.WebApi/
COPY AI.OrderProcessingSystem.Common/AI.OrderProcessingSystem.Common.csproj AI.OrderProcessingSystem.Common/
COPY AI.OrderProcessingSystem.Dal/AI.OrderProcessingSystem.Dal.csproj AI.OrderProcessingSystem.Dal/
COPY AI.OrderProcessingSystem.Worker/AI.OrderProcessingSystem.Worker.csproj AI.OrderProcessingSystem.Worker/
COPY AI.OrderProcessingSystem.CronJob/AI.OrderProcessingSystem.CronJob.csproj AI.OrderProcessingSystem.CronJob/

# Restore dependencies
RUN dotnet restore AI.OrderProcessingSystem.sln

# Copy all source code
COPY . .

# Build the WebApi project
WORKDIR /src/AI.OrderProcessingSystem.WebApi
RUN dotnet build -c Release -o /app/build

# Publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy published files
COPY --from=build /app/publish .

# Copy configuration files
COPY Configuration /Configuration

# Expose ports
EXPOSE 80
EXPOSE 443

# Set environment variables
ENV ASPNETCORE_URLS=http://+:80

ENTRYPOINT ["dotnet", "AI.OrderProcessingSystem.WebApi.dll"]
```

#### 6.2 docker-compose.yml

**\docker-compose.yml**:
```yaml
version: '3.8'

services:
  postgres:
    image: postgres:16-alpine
    container_name: orderprocessing-db
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: SecureP@ssw0rd123
      POSTGRES_DB: orderprocessing
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 10s
      timeout: 5s
      retries: 5
    networks:
      - orderprocessing-network

  webapi:
    build:
      context: .
      dockerfile: Dockerfile
    container_name: orderprocessing-api
    depends_on:
      postgres:
        condition: service_healthy
    ports:
      - "5115:80"
      - "7037:443"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:80
    networks:
      - orderprocessing-network
    volumes:
      - ./Configuration:/Configuration:ro

volumes:
  postgres_data:

networks:
  orderprocessing-network:
    driver: bridge
```

#### 6.3 .dockerignore

**\.dockerignore**:
```
**/.vs
**/.vscode
**/bin
**/obj
**/.git
**/.gitignore
**/.gitattributes
**/Dockerfile
**/docker-compose.yml
**/*.md
**/PRPs
```

---

### Phase 7: Integration Tests

#### 7.1 Create Test Project

**Create new project**:
```bash
dotnet new xunit -n AI.OrderProcessingSystem.WebApi.Tests -f net8.0
```

Add to solution:
```bash
dotnet sln AI.OrderProcessingSystem.sln add AI.OrderProcessingSystem.WebApi.Tests/AI.OrderProcessingSystem.WebApi.Tests.csproj
```

#### 7.2 Test Project Structure

```
AI.OrderProcessingSystem.WebApi.Tests/
  Fixtures/
    WebApplicationFactory.cs
  Tests/
    AuthControllerTests.cs
    UsersControllerTests.cs
    ProductsControllerTests.cs
    OrdersControllerTests.cs
  AI.OrderProcessingSystem.WebApi.Tests.csproj
```

#### 7.3 Sample Test File

**AI.OrderProcessingSystem.WebApi.Tests/Tests/AuthControllerTests.cs**:
```csharp
using System.Net;
using System.Net.Http.Json;
using AI.OrderProcessingSystem.Common.DTOs.Auth;
using Xunit;

namespace AI.OrderProcessingSystem.WebApi.Tests.Tests;

public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        // Arrange
        var loginRequest = new LoginRequestDto
        {
            Email = "admin@orderprocessing.local",
            Password = "Admin@12345"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(result);
        Assert.NotEmpty(result.Token);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = new LoginRequestDto
        {
            Email = "admin@orderprocessing.local",
            Password = "WrongPassword"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithInvalidEmail_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = new LoginRequestDto
        {
            Email = "nonexistent@example.com",
            Password = "SomePassword"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithInvalidEmailFormat_ReturnsBadRequest()
    {
        // Arrange
        var loginRequest = new LoginRequestDto
        {
            Email = "invalid-email",
            Password = "SomePassword"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithMissingPassword_ReturnsBadRequest()
    {
        // Arrange
        var loginRequest = new LoginRequestDto
        {
            Email = "admin@orderprocessing.local",
            Password = ""
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
```

**AI.OrderProcessingSystem.WebApi.Tests/Fixtures/CustomWebApplicationFactory.cs**:
```csharp
using AI.OrderProcessingSystem.Dal.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace AI.OrderProcessingSystem.WebApi.Tests.Fixtures;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("orderprocessing_test")
        .WithUsername("postgres")
        .WithPassword("test")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<OrderProcessingDbContext>));

            if (descriptor != null)
                services.Remove(descriptor);

            // Add test database
            services.AddDbContext<OrderProcessingDbContext>(options =>
            {
                options.UseNpgsql(_dbContainer.GetConnectionString());
            });

            // Ensure database is created and migrated
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OrderProcessingDbContext>();
            db.Database.Migrate();
        });
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
    }
}
```

---

### Phase 8: Database Migrations & Seed Data

#### 8.1 Generate Initial Migration

Before generating the migration, ensure the admin password is hashed:

**Temporary migration helper**:
```bash
# From repository root
cd AI.OrderProcessingSystem.Dal

# Generate BCrypt hash for admin password (using C# Interactive or temporary console app)
# Password: Admin@12345
# Hash: $2a$11$<generated_hash>

# Update DbInitializer.cs with actual hash, then run:
dotnet ef migrations add InitialCreate --startup-project ../AI.OrderProcessingSystem.WebApi

# Apply migration
dotnet ef database update --startup-project ../AI.OrderProcessingSystem.WebApi
```

**IMPORTANT**: The hash for "Admin@12345" must be generated using BCrypt and placed in DbInitializer before running migrations.

---

### Phase 9: Documentation

#### 9.1 README.md

**\README.md**:
```markdown
# AI Order Processing System

A complete REST API system for order processing built with .NET 8, PostgreSQL, and Docker.

## Features

- JWT-based authentication
- CRUD operations for Users, Products, and Orders
- PostgreSQL database with Entity Framework Core
- Swagger/OpenAPI documentation
- Docker containerization
- Integration tests with Testcontainers

## Prerequisites

- .NET 8 SDK
- Docker and Docker Compose
- PostgreSQL 16 (if running locally without Docker)

## Quick Start

### 1. Start the Database and API with Docker Compose

```bash
# From repository root
docker-compose up -d
```

This will:
- Start PostgreSQL on port 5432
- Build and start the WebApi on ports 5115 (HTTP) and 7037 (HTTPS)
- Automatically run migrations and seed data

### 2. Access the API

- **Swagger UI**: http://localhost:5115/swagger
- **Base URL**: http://localhost:5115/api

### 3. Login with Admin Account

**Default Admin Credentials**:
- Email: `admin@orderprocessing.local`
- Password: `Admin@12345`

**Login Request**:
```bash
curl -X POST http://localhost:5115/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@orderprocessing.local",
    "password": "Admin@12345"
  }'
```

**Response**:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2025-12-06T12:00:00Z",
  "user": {
    "id": 1,
    "name": "Administrator",
    "email": "admin@orderprocessing.local"
  }
}
```

### 4. Use the JWT Token

Include the token in subsequent requests:

```bash
curl -X GET http://localhost:5115/api/products \
  -H "Authorization: Bearer YOUR_JWT_TOKEN_HERE"
```

## Manual Setup (Without Docker)

### 1. Start PostgreSQL

Ensure PostgreSQL is running on `localhost:5432` with:
- Database: `orderprocessing`
- Username: `postgres`
- Password: `SecureP@ssw0rd123`

Or update `\Configuration\secrets.json` with your connection string.

### 2. Run Database Migrations

```bash
# From repository root
cd AI.OrderProcessingSystem.Dal
dotnet ef database update --startup-project ../AI.OrderProcessingSystem.WebApi
```

### 3. Run the API

```bash
# From repository root
cd AI.OrderProcessingSystem.WebApi
dotnet run --launch-profile https
```

The API will be available at:
- HTTPS: https://localhost:7037
- HTTP: http://localhost:5115
- Swagger: https://localhost:7037/swagger

## Running Tests

```bash
# From repository root
dotnet test AI.OrderProcessingSystem.WebApi.Tests
```

Tests use Testcontainers to spin up a PostgreSQL instance automatically.

## API Endpoints

### Authentication
- `POST /api/auth/login` - Login and get JWT token

### Users
- `GET /api/users` - Get all users
- `GET /api/users/{id}` - Get user by ID
- `POST /api/users` - Create user
- `PUT /api/users/{id}` - Update user
- `DELETE /api/users/{id}` - Delete user

### Products
- `GET /api/products` - Get all products
- `GET /api/products/{id}` - Get product by ID
- `POST /api/products` - Create product
- `PUT /api/products/{id}` - Update product
- `DELETE /api/products/{id}` - Delete product

### Orders
- `GET /api/orders` - Get all orders
- `GET /api/orders/{id}` - Get order by ID
- `POST /api/orders` - Create order
- `PUT /api/orders/{id}` - Update order status
- `DELETE /api/orders/{id}` - Delete order

All endpoints except `/api/auth/login` require JWT authentication.

## Configuration

Configuration is split into two files in the `\Configuration` directory:

- **secrets.json**: Database credentials, JWT secret, admin password (committed to Git per requirements)
- **instance.json**: Non-sensitive settings like API title, URLs, etc.

## Project Structure

- **AI.OrderProcessingSystem.WebApi**: REST API with controllers and JWT authentication
- **AI.OrderProcessingSystem.Dal**: Data Access Layer with EF Core entities and DbContext
- **AI.OrderProcessingSystem.Common**: Shared DTOs, enums, and constants
- **AI.OrderProcessingSystem.Worker**: Background worker (not used in this phase)
- **AI.OrderProcessingSystem.CronJob**: Scheduled tasks (not used in this phase)
- **AI.OrderProcessingSystem.WebApi.Tests**: Integration tests

## Troubleshooting

**Migration errors**:
```bash
# Reset database
dotnet ef database drop --startup-project ../AI.OrderProcessingSystem.WebApi
dotnet ef database update --startup-project ../AI.OrderProcessingSystem.WebApi
```

**Docker issues**:
```bash
# Rebuild containers
docker-compose down -v
docker-compose up --build
```

**Port conflicts**:
Edit `docker-compose.yml` to change port mappings if 5432, 5115, or 7037 are already in use.

## License

Proprietary - All rights reserved
```

---

## Implementation Task List (Ordered)

Execute these tasks sequentially for successful implementation:

### Phase 1: Project Structure
1. [ ] Convert `AI.OrderProcessingSystem.Dal` from console app to class library (remove OutputType)
2. [ ] Convert `AI.OrderProcessingSystem.Common` from console app to class library (remove OutputType)
3. [ ] Delete `AI.OrderProcessingSystem.Dal/Program.cs`
4. [ ] Delete `AI.OrderProcessingSystem.Common/Program.cs`
5. [ ] Add NuGet packages to `AI.OrderProcessingSystem.Dal`
6. [ ] Add NuGet packages to `AI.OrderProcessingSystem.Common`
7. [ ] Add NuGet packages to `AI.OrderProcessingSystem.WebApi`
8. [ ] Add project references to `AI.OrderProcessingSystem.WebApi`
9. [ ] Add project reference to `AI.OrderProcessingSystem.Dal` (→ Common)
10. [ ] Create `\Configuration` directory
11. [ ] Create `\Configuration\secrets.json`
12. [ ] Create `\Configuration\instance.json`
13. [ ] Run `dotnet restore AI.OrderProcessingSystem.sln`
14. [ ] Run `dotnet build AI.OrderProcessingSystem.sln` to verify

### Phase 2: Database Layer
15. [ ] Create `AI.OrderProcessingSystem.Dal/Entities/User.cs`
16. [ ] Create `AI.OrderProcessingSystem.Dal/Entities/Product.cs`
17. [ ] Create `AI.OrderProcessingSystem.Dal/Entities/Order.cs`
18. [ ] Create `AI.OrderProcessingSystem.Dal/Entities/OrderItem.cs`
19. [ ] Create `AI.OrderProcessingSystem.Dal/Data/OrderProcessingDbContext.cs`
20. [ ] Generate BCrypt hash for admin password
21. [ ] Create `AI.OrderProcessingSystem.Dal/Data/DbInitializer.cs` with hashed password
22. [ ] Update `OrderProcessingDbContext.OnModelCreating` to call DbInitializer

### Phase 3: Shared Layer
23. [ ] Create all DTOs in `AI.OrderProcessingSystem.Common/DTOs/`
24. [ ] Create `AI.OrderProcessingSystem.Common/Enums/OrderStatus.cs`
25. [ ] Create `AI.OrderProcessingSystem.Common/Constants/ValidationConstants.cs` (if needed)

### Phase 4: Authentication Infrastructure
26. [ ] Create `AI.OrderProcessingSystem.WebApi/Configuration/JwtSettings.cs`
27. [ ] Create `AI.OrderProcessingSystem.WebApi/Configuration/AppConfiguration.cs`
28. [ ] Create `AI.OrderProcessingSystem.WebApi/Services/IJwtTokenService.cs`
29. [ ] Create `AI.OrderProcessingSystem.WebApi/Services/JwtTokenService.cs`
30. [ ] Create `AI.OrderProcessingSystem.WebApi/Services/IPasswordService.cs`
31. [ ] Create `AI.OrderProcessingSystem.WebApi/Services/PasswordService.cs`

### Phase 5: API Layer
32. [ ] Replace `AI.OrderProcessingSystem.WebApi/Program.cs` with new implementation
33. [ ] Create `AI.OrderProcessingSystem.WebApi/Controllers/AuthController.cs`
34. [ ] Create `AI.OrderProcessingSystem.WebApi/Controllers/UsersController.cs`
35. [ ] Create `AI.OrderProcessingSystem.WebApi/Controllers/ProductsController.cs`
36. [ ] Create `AI.OrderProcessingSystem.WebApi/Controllers/OrdersController.cs`

### Phase 6: Docker Infrastructure
37. [ ] Create `\Dockerfile`
38. [ ] Create `\docker-compose.yml`
39. [ ] Create `\.dockerignore`

### Phase 7: Database Migrations
40. [ ] Run `dotnet ef migrations add InitialCreate` from Dal project
41. [ ] Verify migration files in `AI.OrderProcessingSystem.Dal/Migrations/`
42. [ ] Run `dotnet ef database update` from Dal project
43. [ ] Verify database and seed data

### Phase 8: Testing
44. [ ] Create test project: `dotnet new xunit -n AI.OrderProcessingSystem.WebApi.Tests`
45. [ ] Add test project to solution
46. [ ] Add NuGet packages to test project
47. [ ] Add project reference to WebApi
48. [ ] Create `CustomWebApplicationFactory.cs`
49. [ ] Create `AuthControllerTests.cs` with 5+ test cases
50. [ ] Create additional test files for Users, Products, Orders
51. [ ] Run `dotnet test` to verify all tests pass

### Phase 9: Documentation & Validation
52. [ ] Create `\README.md`
53. [ ] Run `dotnet build AI.OrderProcessingSystem.sln -warnaserror`
54. [ ] Run `dotnet test AI.OrderProcessingSystem.WebApi.Tests`
55. [ ] Test `docker-compose up` successfully starts services
56. [ ] Verify Swagger UI accessible at http://localhost:5115/swagger
57. [ ] Test login with admin credentials
58. [ ] Test at least one CRUD endpoint with JWT token

---

## Gotchas & Best Practices

### 🔐 Security Considerations

1. **Password Hashing**
   - NEVER store plain text passwords
   - Use BCrypt.Net-Next with default work factor (11)
   - Generate admin password hash BEFORE creating migration
   - Code example:
     ```csharp
     string hash = BCrypt.Net.BCrypt.HashPassword("Admin@12345");
     // $2a$11$... (60 characters)
     ```

2. **JWT Secret Key**
   - Must be at least 256 bits (32 characters) for HS256
   - Use a cryptographically random string
   - Never commit real secrets (though this project does per requirements)
   - Example generation:
     ```csharp
     var bytes = new byte[32];
     RandomNumberGenerator.Fill(bytes);
     var key = Convert.ToBase64String(bytes);
     ```

3. **SQL Injection Protection**
   - EF Core parameterizes queries automatically
   - DO NOT use raw SQL with string concatenation
   - If using `FromSqlRaw`, always use parameterized queries

### 📦 Entity Framework Gotchas

1. **DbContext Lifetime**
   - DbContext is registered as Scoped by default (correct)
   - Never make DbContext Singleton
   - Each HTTP request gets its own DbContext instance

2. **Migration Timing**
   - Option A: Run migrations on app startup (shown in Program.cs with `dbContext.Database.Migrate()`)
   - Option B: Run migrations manually before deployment
   - Production recommendation: Use migration scripts, not auto-migrate

3. **Seed Data with Auto-Increment IDs**
   - When seeding with `HasData`, EF uses specified IDs
   - PostgreSQL sequences must be updated manually if conflicts arise
   - For this project, starting at ID=1 is safe (initial seed)

4. **Check Constraints in PostgreSQL**
   - Use `HasCheckConstraint` for database-level validation
   - Example: `entity.HasCheckConstraint("CK_Product_Price", "price >= 0");`
   - EF migrations will create these in PostgreSQL

5. **Cascade Delete Behavior**
   - Order → OrderItems: Cascade delete (deleting order deletes items)
   - Product → OrderItems: Restrict delete (can't delete product in existing orders)
   - User → Orders: Cascade delete (deleting user deletes their orders)

### 🐳 Docker Considerations

1. **Health Checks**
   - PostgreSQL container has healthcheck to ensure it's ready
   - WebApi depends on `postgres: condition: service_healthy`
   - Prevents race condition where API starts before DB is ready

2. **Volume Persistence**
   - `postgres_data` volume persists database between restarts
   - To reset DB: `docker-compose down -v` (removes volumes)

3. **Configuration File Mounting**
   - Configuration files mounted as read-only volumes
   - Changes to secrets.json require container restart

### 🧪 Testing Best Practices

1. **Testcontainers**
   - Spins up real PostgreSQL instance for tests
   - Isolated from development database
   - Automatically cleaned up after tests

2. **WebApplicationFactory**
   - Creates in-memory test server
   - Allows full integration testing without deploying
   - Override services (like DbContext) for test configuration

3. **Test Data Cleanup**
   - Each test class gets fresh database (via container restart)
   - Use `IClassFixture<CustomWebApplicationFactory>` for test isolation

### ⚠️ Common Pitfalls

1. **Forgetting [Authorize] Attribute**
   - All controllers except AuthController need `[Authorize]`
   - Easy to forget and expose unprotected endpoints

2. **Not Validating Foreign Keys**
   - Always check if User/Product exists before creating Order
   - Return 400 Bad Request with clear message

3. **Stock Management**
   - Decrease stock when creating order
   - Restore stock when deleting order
   - No concurrency control in this version (YAGNI)

4. **Order Total Calculation**
   - Use product price AT TIME OF ORDER (not current price)
   - Store price in OrderItem for historical accuracy

5. **Configuration Loading**
   - Validate configuration at startup
   - Throw exception if required values missing
   - Better to fail fast than runtime errors

---

## Validation Gates

Run these commands to validate the implementation:

### 1. Code Style & Formatting
```bash
dotnet format AI.OrderProcessingSystem.sln --verify-no-changes
```

### 2. Build with Warnings as Errors
```bash
dotnet build AI.OrderProcessingSystem.sln -c Release -warnaserror
```

### 3. Unit & Integration Tests
```bash
dotnet test AI.OrderProcessingSystem.WebApi.Tests --configuration Release --verbosity normal
```

### 4. Docker Build Test
```bash
docker-compose build
```

### 5. End-to-End Smoke Test
```bash
# Start services
docker-compose up -d

# Wait for startup
sleep 10

# Test login
curl -X POST http://localhost:5115/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@orderprocessing.local","password":"Admin@12345"}' \
  | jq -r '.token'

# Cleanup
docker-compose down
```

### 6. Migration Verification
```bash
cd AI.OrderProcessingSystem.Dal
dotnet ef migrations list --startup-project ../AI.OrderProcessingSystem.WebApi
```

All validation gates must pass before considering the implementation complete.

---

## External Resources & Documentation

### Official Microsoft Documentation

- **Entity Framework Core with PostgreSQL**: https://learn.microsoft.com/en-us/ef/core/providers/npgsql/
- **EF Core Migrations**: https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/
- **Data Seeding**: https://learn.microsoft.com/en-us/ef/core/modeling/data-seeding
- **JWT Authentication in ASP.NET Core**: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/jwt-authn
- **Web API Best Practices**: https://learn.microsoft.com/en-us/aspnet/core/web-api/
- **Swashbuckle/OpenAPI**: https://learn.microsoft.com/en-us/aspnet/core/tutorials/getting-started-with-swashbuckle
- **Integration Testing**: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests
- **Dockerizing ASP.NET Core**: https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/docker/

### Third-Party Documentation

- **Npgsql EF Core Provider**: https://www.npgsql.org/efcore/
- **BCrypt.Net**: https://github.com/BcryptNet/bcrypt.net
- **Hellang.Middleware.ProblemDetails**: https://github.com/khellang/Middleware
- **Testcontainers for .NET**: https://dotnet.testcontainers.org/
- **PostgreSQL Docker Image**: https://hub.docker.com/_/postgres

### Example Repositories

- **ASP.NET Core JWT Example**: https://github.com/dotnet/AspNetCore.Docs/tree/main/aspnetcore/security/authentication/jwt-authn
- **EF Core with PostgreSQL Sample**: https://github.com/npgsql/efcore.pg/tree/main/samples

### Helpful Articles

- **JWT Best Practices**: https://tools.ietf.org/html/rfc8725
- **RESTful API Design**: https://restfulapi.net/
- **Problem Details (RFC 7807)**: https://tools.ietf.org/html/rfc7807

---

## Acceptance Criteria

Implementation is complete when:

- [ ] All 5 projects build without warnings
- [ ] Dal and Common are class libraries (not console apps)
- [ ] Configuration files exist and load correctly
- [ ] Database migrations run successfully
- [ ] Admin user exists with hashed password
- [ ] Login endpoint returns valid JWT token
- [ ] All CRUD endpoints work with JWT authentication
- [ ] Swagger UI displays all endpoints with JWT bearer support
- [ ] Input validation returns 400 for invalid data
- [ ] Unauthorized requests return 401
- [ ] Not found requests return 404
- [ ] Docker Compose starts both PostgreSQL and WebApi
- [ ] Minimum 5 integration tests pass
- [ ] README.md documents setup and usage
- [ ] All validation gates pass

---

## Success Metrics & Confidence Score

### Completeness Checklist

- [x] Database schema matches requirements (User, Product, Order, OrderItem)
- [x] All CRUD endpoints implemented
- [x] JWT authentication configured
- [x] Input validation on all DTOs
- [x] Error handling for 400/401/404/500
- [x] Swagger documentation
- [x] Docker configuration
- [x] Integration tests (5+ cases)
- [x] Database migrations
- [x] Seed data for admin user
- [x] README with setup instructions
- [x] Configuration loading (secrets.json + instance.json)
- [x] Password hashing with BCrypt
- [x] Project reference constraints followed

### Risk Assessment

**Low Risk**:
- Standard ASP.NET Core patterns
- Well-documented EF Core setup
- Common JWT implementation
- Docker Compose for PostgreSQL is standard

**Medium Risk**:
- Configuration file loading (manual JSON deserialization) - slightly custom
- BCrypt hash generation timing (must happen before migration)
- Testcontainers setup (requires Docker during tests)

**Mitigations**:
- Detailed code examples provided for all custom logic
- Clear instructions on hash generation
- Fallback: Manual migration + seed script if issues arise

### Implementation Complexity

- **Database Layer**: 4/10 (straightforward EF Core)
- **Authentication**: 5/10 (standard JWT, well-documented)
- **API Controllers**: 3/10 (repetitive CRUD patterns)
- **Docker Setup**: 4/10 (standard docker-compose)
- **Testing**: 6/10 (Testcontainers requires Docker, but examples provided)
- **Configuration**: 6/10 (custom loading, but complete example given)

### Overall Confidence Score: **8.5/10**

**Rationale**:
- Comprehensive examples for every component
- Clear task ordering prevents dependency issues
- All major gotchas documented
- Validation gates catch common errors
- Standard .NET 8 patterns throughout
- External documentation links for reference
- Complete code snippets (not pseudocode)

**Deductions**:
- -1.0: Configuration loading is slightly custom (not built-in)
- -0.5: BCrypt hash generation step requires manual intervention

**Likelihood of one-pass success**: Very high. The PRP provides complete code examples, detailed task ordering, and comprehensive context. An AI agent with basic .NET knowledge should succeed without iteration.
