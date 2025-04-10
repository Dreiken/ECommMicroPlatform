using OrderService.DTOs;
using OrderService.Enums;

namespace OrderService.Services;

public interface IOrderService
{
    Task<OrderDto> CreateOrderAsync(string userId, CreateOrderDto dto);
    Task<OrderDto?> GetOrderAsync(int id);
    Task<IEnumerable<OrderDto>> GetUserOrdersAsync(string userId);
    Task<bool> UpdateOrderStatusAsync(int id, UpdateOrderStatusDto dto);
    Task<bool> DeleteOrderAsync(int id);
}