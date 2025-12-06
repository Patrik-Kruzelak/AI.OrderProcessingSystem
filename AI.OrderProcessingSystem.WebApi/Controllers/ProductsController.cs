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
