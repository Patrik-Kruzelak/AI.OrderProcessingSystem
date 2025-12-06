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
