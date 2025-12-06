using System.Net;
using System.Net.Http.Json;
using AI.OrderProcessingSystem.Common.DTOs.Auth;
using AI.OrderProcessingSystem.WebApi.Tests.Fixtures;

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
