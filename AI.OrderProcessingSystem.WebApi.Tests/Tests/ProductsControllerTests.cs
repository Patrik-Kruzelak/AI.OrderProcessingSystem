using System.Net;
using System.Net.Http.Json;
using AI.OrderProcessingSystem.Common.DTOs.Products;
using AI.OrderProcessingSystem.WebApi.Tests.Fixtures;
using AI.OrderProcessingSystem.WebApi.Tests.Helpers;

namespace AI.OrderProcessingSystem.WebApi.Tests.Tests;

public class ProductsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ProductsControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/products");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_WithAuth_ReturnsProducts()
    {
        // Arrange
        await TestAuthHelper.AuthenticateAsAdminAsync(_client);

        // Act
        var response = await _client.GetAsync("/api/products");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var products = await response.Content.ReadFromJsonAsync<List<ProductResponseDto>>();
        Assert.NotNull(products);
        Assert.NotEmpty(products);
    }

    [Fact]
    public async Task GetById_WithValidId_ReturnsProduct()
    {
        // Arrange
        await TestAuthHelper.AuthenticateAsAdminAsync(_client);

        // Act
        var response = await _client.GetAsync("/api/products/1");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var product = await response.Content.ReadFromJsonAsync<ProductResponseDto>();
        Assert.NotNull(product);
        Assert.Equal(1, product.Id);
        Assert.NotEmpty(product.Name);
    }

    [Fact]
    public async Task GetById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        await TestAuthHelper.AuthenticateAsAdminAsync(_client);

        // Act
        var response = await _client.GetAsync("/api/products/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithValidData_CreatesProduct()
    {
        // Arrange
        await TestAuthHelper.AuthenticateAsAdminAsync(_client);
        var createDto = new CreateProductDto
        {
            Name = "Test Product",
            Description = "Test Description",
            Price = 99.99m,
            Stock = 100
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var product = await response.Content.ReadFromJsonAsync<ProductResponseDto>();
        Assert.NotNull(product);
        Assert.Equal(createDto.Name, product.Name);
        Assert.Equal(createDto.Description, product.Description);
        Assert.Equal(createDto.Price, product.Price);
        Assert.Equal(createDto.Stock, product.Stock);
    }

    [Fact]
    public async Task Create_WithNegativePrice_ReturnsBadRequest()
    {
        // Arrange
        await TestAuthHelper.AuthenticateAsAdminAsync(_client);
        var createDto = new CreateProductDto
        {
            Name = "Test Product",
            Description = "Test Description",
            Price = -10.00m,
            Stock = 100
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithNegativeStock_ReturnsBadRequest()
    {
        // Arrange
        await TestAuthHelper.AuthenticateAsAdminAsync(_client);
        var createDto = new CreateProductDto
        {
            Name = "Test Product",
            Description = "Test Description",
            Price = 99.99m,
            Stock = -5
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithMissingName_ReturnsBadRequest()
    {
        // Arrange
        await TestAuthHelper.AuthenticateAsAdminAsync(_client);
        var createDto = new CreateProductDto
        {
            Name = "",
            Description = "Test Description",
            Price = 99.99m,
            Stock = 100
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithValidData_UpdatesProduct()
    {
        // Arrange
        await TestAuthHelper.AuthenticateAsAdminAsync(_client);

        // Create a product first
        var createDto = new CreateProductDto
        {
            Name = "Original Product",
            Description = "Original Description",
            Price = 50.00m,
            Stock = 50
        };
        var createResponse = await _client.PostAsJsonAsync("/api/products", createDto);
        var createdProduct = await createResponse.Content.ReadFromJsonAsync<ProductResponseDto>();

        // Update the product
        var updateDto = new UpdateProductDto
        {
            Name = "Updated Product",
            Description = "Updated Description",
            Price = 75.00m,
            Stock = 75
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/products/{createdProduct!.Id}", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updatedProduct = await response.Content.ReadFromJsonAsync<ProductResponseDto>();
        Assert.NotNull(updatedProduct);
        Assert.Equal("Updated Product", updatedProduct.Name);
        Assert.Equal("Updated Description", updatedProduct.Description);
        Assert.Equal(75.00m, updatedProduct.Price);
        Assert.Equal(75, updatedProduct.Stock);
    }

    [Fact]
    public async Task Update_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        await TestAuthHelper.AuthenticateAsAdminAsync(_client);
        var updateDto = new UpdateProductDto
        {
            Name = "Updated Product",
            Description = "Updated Description",
            Price = 75.00m,
            Stock = 75
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/products/99999", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithNegativePrice_ReturnsBadRequest()
    {
        // Arrange
        await TestAuthHelper.AuthenticateAsAdminAsync(_client);

        // Create a product first
        var createDto = new CreateProductDto
        {
            Name = "Test Product",
            Description = "Test Description",
            Price = 50.00m,
            Stock = 50
        };
        var createResponse = await _client.PostAsJsonAsync("/api/products", createDto);
        var createdProduct = await createResponse.Content.ReadFromJsonAsync<ProductResponseDto>();

        // Try to update with negative price
        var updateDto = new UpdateProductDto
        {
            Name = "Updated Product",
            Description = "Updated Description",
            Price = -10.00m,
            Stock = 50
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/products/{createdProduct!.Id}", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WithValidId_DeletesProduct()
    {
        // Arrange
        await TestAuthHelper.AuthenticateAsAdminAsync(_client);

        // Create a product first
        var createDto = new CreateProductDto
        {
            Name = "To Delete",
            Description = "Will be deleted",
            Price = 10.00m,
            Stock = 10
        };
        var createResponse = await _client.PostAsJsonAsync("/api/products", createDto);
        var createdProduct = await createResponse.Content.ReadFromJsonAsync<ProductResponseDto>();

        // Act
        var response = await _client.DeleteAsync($"/api/products/{createdProduct!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify product is deleted
        var getResponse = await _client.GetAsync($"/api/products/{createdProduct.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        await TestAuthHelper.AuthenticateAsAdminAsync(_client);

        // Act
        var response = await _client.DeleteAsync("/api/products/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateAndRetrieve_VerifiesCreatedAtTimestamp()
    {
        // Arrange
        await TestAuthHelper.AuthenticateAsAdminAsync(_client);
        var beforeCreate = DateTime.UtcNow;

        var createDto = new CreateProductDto
        {
            Name = "Timestamp Test Product",
            Description = "Test Description",
            Price = 99.99m,
            Stock = 100
        };

        // Act
        var createResponse = await _client.PostAsJsonAsync("/api/products", createDto);
        var afterCreate = DateTime.UtcNow;

        // Assert
        var product = await createResponse.Content.ReadFromJsonAsync<ProductResponseDto>();
        Assert.NotNull(product);
        Assert.True(product.CreatedAt >= beforeCreate && product.CreatedAt <= afterCreate);
    }
}
