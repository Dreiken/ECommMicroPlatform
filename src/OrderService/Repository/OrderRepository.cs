using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Models;
using OrderService.Enums;
using OrderService.DTOs;

namespace OrderService.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _context;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<OrderRepository> _logger;

    public OrderRepository(
        AppDbContext context,
        IEventPublisher eventPublisher,
        ILogger<OrderRepository> logger)
    {
        _context = context;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<Order> CreateAsync(Order order)
    {
        _logger.LogInformation("Creating order for user {UserId} with product {ProductId}", order.UserId, order.ProductId);
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        await _eventPublisher.PublishOrderCreatedEventAsync(order);
        return order;
    }

    public async Task<Order?> GetByIdAsync(int id)
    {
        _logger.LogInformation("Fetching order with ID {OrderId}", id);
        return await _context.Orders.FindAsync(id);
    }

    public async Task<IEnumerable<Order>> GetByUserIdAsync(string userId)
    {
        _logger.LogInformation("Fetching orders for user {UserId}", userId);
        return await _context.Orders
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Order>> GetByProductIdAsync(string productId)
    {
        _logger.LogInformation("Fetching orders for product {ProductId}", productId);
        return await _context.Orders
            .Where(o => o.ProductId == productId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }
    

    public async Task<bool> UpdateStatusAsync(int id, OrderStatus status)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null) return false;

        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow;
        
        _context.Orders.Update(order);
        var updated = await _context.SaveChangesAsync() > 0;

        if (updated)
        {
            await _eventPublisher.PublishOrderStatusUpdatedEventAsync(order);
        }

        return updated;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        _logger.LogInformation("Deleting order with ID {OrderId}", id);
        var order = await _context.Orders.FindAsync(id);
        if (order == null) return false;

        _context.Orders.Remove(order);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        _logger.LogInformation("Checking existence of order with ID {OrderId}", id);
        return await _context.Orders.AnyAsync(o => o.Id == id);
    }
}