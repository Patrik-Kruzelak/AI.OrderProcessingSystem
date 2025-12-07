using AI.OrderProcessingSystem.Common.Abstractions;
using AI.OrderProcessingSystem.Common.DTOs.Orders;
using AI.OrderProcessingSystem.Common.Events;
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
    private readonly IEventPublisher _eventPublisher;

    public OrdersController(
        OrderProcessingDbContext context,
        ILogger<OrdersController> logger,
        IEventPublisher eventPublisher)
    {
        _context = context;
        _logger = logger;
        _eventPublisher = eventPublisher;
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

        // Publish OrderCreatedEvent
        var orderCreatedEvent = new OrderCreatedEvent
        {
            OrderId = order.Id,
            UserId = order.UserId,
            Total = order.Total,
            CreatedAt = order.CreatedAt
        };
        await _eventPublisher.PublishAsync(orderCreatedEvent);

        // Create notification
        var notification = new Notification
        {
            OrderId = order.Id,
            EventType = "OrderCreated",
            Message = $"Order #{order.Id} created with total ${order.Total:F2}",
            IsEmailSent = false,
            CreatedAt = DateTime.UtcNow
        };
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

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

        var oldStatus = order.Status;
        order.Status = dto.Status.ToLower();
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Order {OrderId} updated to status {Status}", order.Id, order.Status);

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
