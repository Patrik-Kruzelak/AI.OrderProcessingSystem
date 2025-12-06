using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AI.OrderProcessingSystem.Dal.Entities;
using AI.OrderProcessingSystem.WebApi.Configuration;
using AI.OrderProcessingSystem.WebApi.Services;
using Microsoft.IdentityModel.Tokens;

namespace AI.OrderProcessingSystem.WebApi.Tests.Tests;

public class JwtTokenServiceTests
{
    private readonly JwtTokenService _jwtTokenService;
    private readonly JwtSettings _jwtSettings;

    public JwtTokenServiceTests()
    {
        _jwtSettings = new JwtSettings
        {
            SecretKey = "test-secret-key-with-minimum-32-characters-for-hs256-algorithm",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpirationMinutes = 60
        };
        _jwtTokenService = new JwtTokenService(_jwtSettings);
    }

    [Fact]
    public void GenerateToken_ReturnsNonEmptyToken()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Name = "Test User",
            Email = "test@example.com",
            Password = "hashedpassword"
        };

        // Act
        var token = _jwtTokenService.GenerateToken(user);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }

    [Fact]
    public void GenerateToken_CreatesValidJwtFormat()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Name = "Test User",
            Email = "test@example.com",
            Password = "hashedpassword"
        };

        // Act
        var token = _jwtTokenService.GenerateToken(user);

        // Assert - JWT tokens have 3 parts separated by dots
        var parts = token.Split('.');
        Assert.Equal(3, parts.Length);
    }

    [Fact]
    public void GenerateToken_ContainsCorrectClaims()
    {
        // Arrange
        var user = new User
        {
            Id = 123,
            Name = "Test User",
            Email = "test@example.com",
            Password = "hashedpassword"
        };

        // Act
        var token = _jwtTokenService.GenerateToken(user);

        // Assert - Decode and verify claims
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        Assert.Equal("123", jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal("test@example.com", jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal("Test User", jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Name).Value);
        Assert.NotNull(jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti));
    }

    [Fact]
    public void GenerateToken_ContainsCorrectIssuerAndAudience()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Name = "Test User",
            Email = "test@example.com",
            Password = "hashedpassword"
        };

        // Act
        var token = _jwtTokenService.GenerateToken(user);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        Assert.Equal(_jwtSettings.Issuer, jwtToken.Issuer);
        Assert.Contains(_jwtSettings.Audience, jwtToken.Audiences);
    }

    [Fact]
    public void GenerateToken_HasCorrectExpirationTime()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Name = "Test User",
            Email = "test@example.com",
            Password = "hashedpassword"
        };
        var beforeGeneration = DateTime.UtcNow;

        // Act
        var token = _jwtTokenService.GenerateToken(user);
        var afterGeneration = DateTime.UtcNow;

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var expectedMinExpiration = beforeGeneration.AddMinutes(_jwtSettings.ExpirationMinutes);
        var expectedMaxExpiration = afterGeneration.AddMinutes(_jwtSettings.ExpirationMinutes);

        Assert.True(jwtToken.ValidTo >= expectedMinExpiration);
        Assert.True(jwtToken.ValidTo <= expectedMaxExpiration);
    }

    [Fact]
    public void GenerateToken_DifferentUsersProduceDifferentTokens()
    {
        // Arrange
        var user1 = new User
        {
            Id = 1,
            Name = "User One",
            Email = "user1@example.com",
            Password = "hashedpassword"
        };
        var user2 = new User
        {
            Id = 2,
            Name = "User Two",
            Email = "user2@example.com",
            Password = "hashedpassword"
        };

        // Act
        var token1 = _jwtTokenService.GenerateToken(user1);
        var token2 = _jwtTokenService.GenerateToken(user2);

        // Assert
        Assert.NotEqual(token1, token2);
    }

    [Fact]
    public void GenerateToken_SameUserProducesDifferentTokensDueToJti()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Name = "Test User",
            Email = "test@example.com",
            Password = "hashedpassword"
        };

        // Act
        var token1 = _jwtTokenService.GenerateToken(user);
        var token2 = _jwtTokenService.GenerateToken(user);

        // Assert
        // Tokens should be different because Jti (JWT ID) is unique each time
        Assert.NotEqual(token1, token2);
    }

    [Fact]
    public void GenerateToken_CanBeValidatedWithCorrectKey()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Name = "Test User",
            Email = "test@example.com",
            Password = "hashedpassword"
        };

        // Act
        var token = _jwtTokenService.GenerateToken(user);

        // Assert - Validate token
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = _jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = _jwtSettings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
        Assert.NotNull(principal);
        Assert.NotNull(validatedToken);
    }

    [Fact]
    public void GetTokenExpiration_ReturnsCorrectExpiration()
    {
        // Arrange
        var beforeCall = DateTime.UtcNow;

        // Act
        var expiration = _jwtTokenService.GetTokenExpiration();
        var afterCall = DateTime.UtcNow;

        // Assert
        var expectedMinExpiration = beforeCall.AddMinutes(_jwtSettings.ExpirationMinutes);
        var expectedMaxExpiration = afterCall.AddMinutes(_jwtSettings.ExpirationMinutes);

        Assert.True(expiration >= expectedMinExpiration);
        Assert.True(expiration <= expectedMaxExpiration);
    }

    [Fact]
    public void GenerateToken_WithSpecialCharactersInName_WorksCorrectly()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Name = "Test User with Special Chars !@#$%",
            Email = "test@example.com",
            Password = "hashedpassword"
        };

        // Act
        var token = _jwtTokenService.GenerateToken(user);

        // Assert
        Assert.NotNull(token);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        Assert.Equal(user.Name, jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Name).Value);
    }

    [Fact]
    public void GenerateToken_WithLongUserName_WorksCorrectly()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Name = new string('A', 200),
            Email = "test@example.com",
            Password = "hashedpassword"
        };

        // Act
        var token = _jwtTokenService.GenerateToken(user);

        // Assert
        Assert.NotNull(token);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        Assert.Equal(user.Name, jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Name).Value);
    }

    [Theory]
    [InlineData(1, "User1", "user1@example.com")]
    [InlineData(999, "User999", "user999@example.com")]
    [InlineData(12345, "Admin User", "admin@example.com")]
    public void GenerateToken_WithVariousUsers_CreatesValidTokens(int userId, string name, string email)
    {
        // Arrange
        var user = new User
        {
            Id = userId,
            Name = name,
            Email = email,
            Password = "hashedpassword"
        };

        // Act
        var token = _jwtTokenService.GenerateToken(user);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        Assert.Equal(userId.ToString(), jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal(email, jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal(name, jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Name).Value);
    }
}
