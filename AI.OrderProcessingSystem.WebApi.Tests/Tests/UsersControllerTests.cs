using System.Net;
using System.Net.Http.Json;
using AI.OrderProcessingSystem.Common.DTOs.Users;
using AI.OrderProcessingSystem.WebApi.Tests.Fixtures;
using AI.OrderProcessingSystem.WebApi.Tests.Helpers;

namespace AI.OrderProcessingSystem.WebApi.Tests.Tests;

public class UsersControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public UsersControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/users");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_WithAuth_ReturnsUsers()
    {
        // Arrange
        await TestAuthHelper.AuthenticateAsAdminAsync(_client);

        // Act
        var response = await _client.GetAsync("/api/users");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var users = await response.Content.ReadFromJsonAsync<List<UserResponseDto>>();
        Assert.NotNull(users);
        Assert.NotEmpty(users);
    }

    [Fact]
    public async Task GetById_WithValidId_ReturnsUser()
    {
        // Arrange
        await TestAuthHelper.AuthenticateAsAdminAsync(_client);

        // Act
        var response = await _client.GetAsync("/api/users/1");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserResponseDto>();
        Assert.NotNull(user);
        Assert.Equal(1, user.Id);
        Assert.Equal("admin@orderprocessing.local", user.Email);
    }

    [Fact]
    public async Task GetById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        await TestAuthHelper.AuthenticateAsAdminAsync(_client);

        // Act
        var response = await _client.GetAsync("/api/users/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithValidData_CreatesUser()
    {
        // Arrange
        await TestAuthHelper.AuthenticateAsAdminAsync(_client);
        var createDto = new CreateUserDto
        {
            Name = "Test User",
            Email = $"test.{Guid.NewGuid()}@example.com",
            Password = "Test@12345"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/users", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserResponseDto>();
        Assert.NotNull(user);
        Assert.Equal(createDto.Name, user.Name);
        Assert.Equal(createDto.Email, user.Email);
    }

    [Fact]
    public async Task Create_WithDuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        await TestAuthHelper.AuthenticateAsAdminAsync(_client);
        var createDto = new CreateUserDto
        {
            Name = "Duplicate User",
            Email = "admin@orderprocessing.local", // Existing email
            Password = "Test@12345"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/users", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithInvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        await TestAuthHelper.AuthenticateAsAdminAsync(_client);
        var createDto = new CreateUserDto
        {
            Name = "Test User",
            Email = "invalid-email",
            Password = "Test@12345"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/users", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithMissingPassword_ReturnsBadRequest()
    {
        // Arrange
        await TestAuthHelper.AuthenticateAsAdminAsync(_client);
        var createDto = new CreateUserDto
        {
            Name = "Test User",
            Email = $"test.{Guid.NewGuid()}@example.com",
            Password = ""
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/users", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithValidData_UpdatesUser()
    {
        // Arrange
        await TestAuthHelper.AuthenticateAsAdminAsync(_client);

        // Create a user first
        var createDto = new CreateUserDto
        {
            Name = "Original Name",
            Email = $"update.{Guid.NewGuid()}@example.com",
            Password = "Test@12345"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/users", createDto);
        var createdUser = await createResponse.Content.ReadFromJsonAsync<UserResponseDto>();

        // Update the user
        var updateDto = new UpdateUserDto
        {
            Name = "Updated Name",
            Email = createdUser!.Email,
            Password = null  // Don't change the password
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/users/{createdUser.Id}", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updatedUser = await response.Content.ReadFromJsonAsync<UserResponseDto>();
        Assert.NotNull(updatedUser);
        Assert.Equal("Updated Name", updatedUser.Name);
    }

    [Fact]
    public async Task Update_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        await TestAuthHelper.AuthenticateAsAdminAsync(_client);
        var updateDto = new UpdateUserDto
        {
            Name = "Updated Name",
            Email = "test@example.com",
            Password = null  // Don't change the password
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/users/99999", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithDuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        await TestAuthHelper.AuthenticateAsAdminAsync(_client);

        // Create first user
        var createDto1 = new CreateUserDto
        {
            Name = "User 1",
            Email = $"user1.{Guid.NewGuid()}@example.com",
            Password = "Test@12345"
        };
        var createResponse1 = await _client.PostAsJsonAsync("/api/users", createDto1);
        var user1 = await createResponse1.Content.ReadFromJsonAsync<UserResponseDto>();

        // Try to update with admin email (existing)
        var updateDto = new UpdateUserDto
        {
            Name = user1!.Name,
            Email = "admin@orderprocessing.local", // Existing email
            Password = ""
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/users/{user1.Id}", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WithValidId_DeletesUser()
    {
        // Arrange
        await TestAuthHelper.AuthenticateAsAdminAsync(_client);

        // Create a user first
        var createDto = new CreateUserDto
        {
            Name = "To Delete",
            Email = $"delete.{Guid.NewGuid()}@example.com",
            Password = "Test@12345"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/users", createDto);
        var createdUser = await createResponse.Content.ReadFromJsonAsync<UserResponseDto>();

        // Act
        var response = await _client.DeleteAsync($"/api/users/{createdUser!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify user is deleted
        var getResponse = await _client.GetAsync($"/api/users/{createdUser.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        await TestAuthHelper.AuthenticateAsAdminAsync(_client);

        // Act
        var response = await _client.DeleteAsync("/api/users/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
