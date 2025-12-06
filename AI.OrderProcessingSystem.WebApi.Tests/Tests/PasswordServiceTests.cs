using AI.OrderProcessingSystem.WebApi.Services;

namespace AI.OrderProcessingSystem.WebApi.Tests.Tests;

public class PasswordServiceTests
{
    private readonly PasswordService _passwordService;

    public PasswordServiceTests()
    {
        _passwordService = new PasswordService();
    }

    [Fact]
    public void HashPassword_ReturnsNonEmptyHash()
    {
        // Arrange
        var password = "TestPassword123";

        // Act
        var hash = _passwordService.HashPassword(password);

        // Assert
        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
    }

    [Fact]
    public void HashPassword_DifferentPasswordsProduceDifferentHashes()
    {
        // Arrange
        var password1 = "Password1";
        var password2 = "Password2";

        // Act
        var hash1 = _passwordService.HashPassword(password1);
        var hash2 = _passwordService.HashPassword(password2);

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void HashPassword_SamePasswordProducesDifferentHashesDueToSalt()
    {
        // Arrange
        var password = "TestPassword123";

        // Act
        var hash1 = _passwordService.HashPassword(password);
        var hash2 = _passwordService.HashPassword(password);

        // Assert
        // BCrypt uses different salts, so hashes should be different
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ReturnsTrue()
    {
        // Arrange
        var password = "TestPassword123";
        var hash = _passwordService.HashPassword(password);

        // Act
        var result = _passwordService.VerifyPassword(password, hash);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_WithIncorrectPassword_ReturnsFalse()
    {
        // Arrange
        var correctPassword = "CorrectPassword";
        var incorrectPassword = "WrongPassword";
        var hash = _passwordService.HashPassword(correctPassword);

        // Act
        var result = _passwordService.VerifyPassword(incorrectPassword, hash);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyPassword_WithEmptyPassword_ReturnsFalse()
    {
        // Arrange
        var password = "TestPassword123";
        var hash = _passwordService.HashPassword(password);

        // Act
        var result = _passwordService.VerifyPassword("", hash);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyPassword_CaseSensitive()
    {
        // Arrange
        var password = "TestPassword123";
        var hash = _passwordService.HashPassword(password);

        // Act
        var result = _passwordService.VerifyPassword("testpassword123", hash);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HashPassword_WithSpecialCharacters_WorksCorrectly()
    {
        // Arrange
        var password = "P@ssw0rd!#$%^&*()";

        // Act
        var hash = _passwordService.HashPassword(password);
        var result = _passwordService.VerifyPassword(password, hash);

        // Assert
        Assert.NotNull(hash);
        Assert.True(result);
    }

    [Fact]
    public void HashPassword_WithLongPassword_WorksCorrectly()
    {
        // Arrange
        var password = new string('a', 200);

        // Act
        var hash = _passwordService.HashPassword(password);
        var result = _passwordService.VerifyPassword(password, hash);

        // Assert
        Assert.NotNull(hash);
        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_WithKnownBCryptHash_ReturnsTrue()
    {
        // Arrange - Using a pre-generated BCrypt hash for "Admin@12345"
        var password = "Admin@12345";
        var knownHash = "$2a$11$GCjXCtewd44o5yTAEMMi6OdRppjz8cekQO1bsyofoIlyLUpBlFG5m";

        // Act
        var result = _passwordService.VerifyPassword(password, knownHash);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("password123")]
    [InlineData("P@ssw0rd")]
    [InlineData("Admin@12345")]
    [InlineData("Test!23456")]
    public void HashAndVerify_WithVariousPasswords_WorksCorrectly(string password)
    {
        // Arrange & Act
        var hash = _passwordService.HashPassword(password);
        var result = _passwordService.VerifyPassword(password, hash);

        // Assert
        Assert.True(result);
    }
}
