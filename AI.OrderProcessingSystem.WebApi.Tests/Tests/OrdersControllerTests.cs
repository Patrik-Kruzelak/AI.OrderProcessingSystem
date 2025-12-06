using System.Net;
using System.Net.Http.Json;
using AI.OrderProcessingSystem.Common.DTOs.Orders;
using AI.OrderProcessingSystem.Common.DTOs.Products;
using AI.OrderProcessingSystem.WebApi.Tests.Fixtures;
using AI.OrderProcessingSystem.WebApi.Tests.Helpers;

namespace AI.OrderProcessingSystem.WebApi.Tests.Tests;

public class OrdersControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public OrdersControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/orders");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_WithAuth_ReturnsOrders()
    {
        // Arrange
        await TestAuthHelper.AuthenticateAsAdminAsync(_client);

        // Act
        var response = await _client.GetAsync("/api/orders");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var orders = await response.Content.ReadFromJsonAsync<List<OrderResponseDto>>();
        Assert.NotNull(orders);
    }

    [Fact]
    public async Task GetById_WithValidId_ReturnsOrder()
    {
        // Arrange
        await TestAuthHelper.AuthenticateAsAdminAsync(_client);

        // Create an order first
        var createDto = new CreateOrderDto
        {
            UserId = 1,
            Items = new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto { ProductId = 1, Quantity = 2 }
            }
        };
        var createResponse = await _client.PostAsJsonAsync("/api/orders", createDto);
        var createdOrder = await createResponse.Content.ReadFromJsonAsync<OrderResponseDto>();

        // Act
        var response = await _client.GetAsync($"/api/orders/{createdOrder!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<OrderResponseDto>();
        Assert.NotNull(order);
        Assert.Equal(createdOrder.Id, order.Id);
        Assert.NotEmpty(order.Items);
    }

    [Fact]
    public async Task GetById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        await TestAuthHelper.AuthenticateAsAdminAsync(_client);

        // Act
        var response = await _client.GetAsync("/api/orders/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithValidData_CreatesOrder()
    {
        // Arrange
        await TestAuthHelper.AuthenticateAsAdminAsync(_client);

        var createDto = new CreateOrderDto
        {
            UserId = 1,
            Items = new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto { ProductId = 1, Quantity = 1 }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<OrderResponseDto>();
        Assert.NotNull(order);
        Assert.Equal(1, order.UserId);
        Assert.Single(order.Items);
        Assert.Equal("pending", order.Status);
        Assert.True(order.Total > 0);
    }

    [Fact]
    public async Task Create_WithMultipleItems_CalculatesTotalCorrectly()
    {
        // Arrange
        await TestAuthHelper.AuthenticateAsAdminAsync(_client);

        // Get product prices first
        var product1Response = await _client.GetAsync("/api/products/1");
        var product1 = await product1Response.Content.ReadFromJsonAsync<ProductResponseDto>();
        var product2Response = await _client.GetAsync("/api/products/2");
        var product2 = await product2Response.Content.ReadFromJsonAsync<ProductResponseDto>();

        var createDto = new CreateOrderDto
        {
            UserId = 1,
            Items = new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto { ProductId = 1, Quantity = 2 },
                new CreateOrderItemDto { ProductId = 2, Quantity = 1 }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<OrderResponseDto>();
        Assert.NotNull(order);
        Assert.Equal(2, order.Items.Count);

        var expectedTotal = (product1!.Price * 2) + (product2!.Price * 1);
        Assert.Equal(expectedTotal, order.Total);
    }

    [Fact]
    public async Task Create_WithInvalidUserId_ReturnsBadRequest()
    {
        // Arrange
        await TestAuthHelper.AuthenticateAsAdminAsync(_client);

        var createDto = new CreateOrderDto
        {
            UserId = 99999,
            Items = new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto { ProductId = 1, Quantity = 1 }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithInvalidProductId_ReturnsBadRequest()
    {
        // Arrange
        await TestAuthHelper.AuthenticateAsAdminAsync(_client);

        var createDto = new CreateOrderDto
        {
            UserId = 1,
            Items = new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto { ProductId = 99999, Quantity = 1 }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithInsufficientStock_ReturnsBadRequest()
    {
        // Arrange
        await TestAuthHelper.AuthenticateAsAdminAsync(_client);

        // Get current stock for product 1
        var productResponse = await _client.GetAsync("/api/products/1");
        var product = await productResponse.Content.ReadFromJsonAsync<ProductResponseDto>();

        var createDto = new CreateOrderDto
        {
            UserId = 1,
            Items = new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto { ProductId = 1, Quantity = product!.Stock + 100 }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_DecreasesProductStock()
    {
        // Arrange
        await TestAuthHelper.AuthenticateAsAdminAsync(_client);

        // Get initial stock
        var initialProductResponse = await _client.GetAsync("/api/products/1");
        var initialProduct = await initialProductResponse.Content.ReadFromJsonAsync<ProductResponseDto>();
        var initialStock = initialProduct!.Stock;

        var quantity = 3;
        var createDto = new CreateOrderDto
        {
            UserId = 1,
            Items = new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto { ProductId = 1, Quantity = quantity }
            }
        };

        // Act
        await _client.PostAsJsonAsync("/api/orders", createDto);

        // Assert
        var finalProductResponse = await _client.GetAsync("/api/products/1");
        var finalProduct = await finalProductResponse.Content.ReadFromJsonAsync<ProductResponseDto>();
        Assert.Equal(initialStock - quantity, finalProduct!.Stock);
    }

    [Fact]
    public async Task Update_WithValidStatus_UpdatesOrder()
    {
        // Arrange
        await TestAuthHelper.AuthenticateAsAdminAsync(_client);

        // Create an order first
        var createDto = new CreateOrderDto
        {
            UserId = 1,
            Items = new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto { ProductId = 1, Quantity = 1 }
            }
        };
        var createResponse = await _client.PostAsJsonAsync("/api/orders", createDto);
        var createdOrder = await createResponse.Content.ReadFromJsonAsync<OrderResponseDto>();

        // Update status
        var updateDto = new UpdateOrderDto
        {
            Status = "completed"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/orders/{createdOrder!.Id}", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updatedOrder = await response.Content.ReadFromJsonAsync<OrderResponseDto>();
        Assert.NotNull(updatedOrder);
        Assert.Equal("completed", updatedOrder.Status);
    }

    [Fact]
    public async Task Update_WithInvalidStatus_ReturnsBadRequest()
    {
        // Arrange
        await TestAuthHelper.AuthenticateAsAdminAsync(_client);

        // Create an order first
        var createDto = new CreateOrderDto
        {
            UserId = 1,
            Items = new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto { ProductId = 1, Quantity = 1 }
            }
        };
        var createResponse = await _client.PostAsJsonAsync("/api/orders", createDto);
        var createdOrder = await createResponse.Content.ReadFromJsonAsync<OrderResponseDto>();

        // Update with invalid status
        var updateDto = new UpdateOrderDto
        {
            Status = "invalid-status"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/orders/{createdOrder!.Id}", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        await TestAuthHelper.AuthenticateAsAdminAsync(_client);

        var updateDto = new UpdateOrderDto
        {
            Status = "completed"
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/orders/99999", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WithValidId_DeletesOrderAndRestoresStock()
    {
        // Arrange
        await TestAuthHelper.AuthenticateAsAdminAsync(_client);

        // Get initial stock
        var initialProductResponse = await _client.GetAsync("/api/products/1");
        var initialProduct = await initialProductResponse.Content.ReadFromJsonAsync<ProductResponseDto>();
        var initialStock = initialProduct!.Stock;

        // Create an order
        var quantity = 5;
        var createDto = new CreateOrderDto
        {
            UserId = 1,
            Items = new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto { ProductId = 1, Quantity = quantity }
            }
        };
        var createResponse = await _client.PostAsJsonAsync("/api/orders", createDto);
        var createdOrder = await createResponse.Content.ReadFromJsonAsync<OrderResponseDto>();

        // Act - Delete the order
        var deleteResponse = await _client.DeleteAsync($"/api/orders/{createdOrder!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Verify order is deleted
        var getResponse = await _client.GetAsync($"/api/orders/{createdOrder.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);

        // Verify stock is restored
        var finalProductResponse = await _client.GetAsync("/api/products/1");
        var finalProduct = await finalProductResponse.Content.ReadFromJsonAsync<ProductResponseDto>();
        Assert.Equal(initialStock, finalProduct!.Stock);
    }

    [Fact]
    public async Task Delete_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        await TestAuthHelper.AuthenticateAsAdminAsync(_client);

        // Act
        var response = await _client.DeleteAsync("/api/orders/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithEmptyItems_ReturnsBadRequest()
    {
        // Arrange
        await TestAuthHelper.AuthenticateAsAdminAsync(_client);

        var createDto = new CreateOrderDto
        {
            UserId = 1,
            Items = new List<CreateOrderItemDto>()
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task OrderLifecycle_CreateUpdateDelete_WorksCorrectly()
    {
        // Arrange
        await TestAuthHelper.AuthenticateAsAdminAsync(_client);

        // Create
        var createDto = new CreateOrderDto
        {
            UserId = 1,
            Items = new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto { ProductId = 1, Quantity = 1 }
            }
        };
        var createResponse = await _client.PostAsJsonAsync("/api/orders", createDto);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var order = await createResponse.Content.ReadFromJsonAsync<OrderResponseDto>();

        // Update to processing
        var updateDto1 = new UpdateOrderDto { Status = "processing" };
        var updateResponse1 = await _client.PutAsJsonAsync($"/api/orders/{order!.Id}", updateDto1);
        Assert.Equal(HttpStatusCode.OK, updateResponse1.StatusCode);
        var updatedOrder1 = await updateResponse1.Content.ReadFromJsonAsync<OrderResponseDto>();
        Assert.Equal("processing", updatedOrder1!.Status);

        // Update to completed
        var updateDto2 = new UpdateOrderDto { Status = "completed" };
        var updateResponse2 = await _client.PutAsJsonAsync($"/api/orders/{order.Id}", updateDto2);
        Assert.Equal(HttpStatusCode.OK, updateResponse2.StatusCode);
        var updatedOrder2 = await updateResponse2.Content.ReadFromJsonAsync<OrderResponseDto>();
        Assert.Equal("completed", updatedOrder2!.Status);

        // Delete
        var deleteResponse = await _client.DeleteAsync($"/api/orders/{order.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }
}
