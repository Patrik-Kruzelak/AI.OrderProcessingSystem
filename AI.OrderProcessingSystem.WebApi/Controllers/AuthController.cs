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
