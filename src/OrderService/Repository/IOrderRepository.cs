using OrderService.Enums;
using OrderService.Models;

namespace OrderService.Repositories;

public interface IOrderRepository
{
    Task<Order> CreateAsync(Order order);
    Task<Order?> GetByIdAsync(int id);
    Task<IEnumerable<Order>> GetByUserIdAsync(string userId);
    Task<IEnumerable<Order>> GetByProductIdAsync(string productId);
    Task<bool> UpdateStatusAsync(int id, OrderStatus status);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
}