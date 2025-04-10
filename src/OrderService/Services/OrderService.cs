using OrderService.DTOs;
using OrderService.Models;
using OrderService.Repositories;
using RabbitMQ.Client;
using System.Text.Json;
using System.Text;
using OrderService.Enums;

namespace OrderService.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IConnection _rabbitConnection;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IOrderRepository orderRepository,
        ILogger<OrderService> logger)
    {
        _orderRepository = orderRepository;
        _logger = logger;
    }

    public async Task<OrderDto> CreateOrderAsync(string userId, CreateOrderDto dto)
    {
        _logger.LogInformation("SERVICE: Creating order for user {UserId} with product {ProductId}", userId, dto.ProductId);
        var order = new Order
        {
            UserId = userId,
            ProductId = dto.ProductId,
            Quantity = dto.Quantity,
            Status = OrderStatus.Pending,
            TotalPrice = 0
        };

        var createdOrder = await _orderRepository.CreateAsync(order);
        return MapToDto(createdOrder);
    }


    public async Task<OrderDto?> GetOrderAsync(int id)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        return order != null ? MapToDto(order) : null;
    }

    public async Task<IEnumerable<OrderDto>> GetUserOrdersAsync(string userId)
    {
        var orders = await _orderRepository.GetByUserIdAsync(userId);
        return orders.Select(MapToDto);
    }

    public async Task<bool> UpdateOrderStatusAsync(int id, UpdateOrderStatusDto dto)
    {
        try
        {
            var order = await _orderRepository.GetByIdAsync(id);
            if (order == null)
            {
                _logger.LogWarning("Failed to update order {OrderId} status - order not found", id);
                return false;
            }

            if (!IsValidStatusTransition(order.Status, dto.Status))
            {
                _logger.LogWarning(
                    "Invalid status transition attempted for order {OrderId}: {CurrentStatus} -> {NewStatus}", 
                    id, 
                    order.Status, 
                    dto.Status
                );
                throw new InvalidOperationException(
                    $"Invalid status transition from {order.Status} to {dto.Status}"
                );
            }

            var updated = await _orderRepository.UpdateStatusAsync(id, dto.Status);
            if (updated)
            {
                _logger.LogInformation(
                    "Order {OrderId} status updated to {Status}. Reason: {Reason}", 
                    id, 
                    dto.Status,
                    dto.Reason ?? "No reason provided"
                );
            }

            return updated;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex, 
                "Error updating status for order {OrderId} to {Status}", 
                id, 
                dto.Status
            );
            throw;
        }
    }

    public async Task<bool> DeleteOrderAsync(int id)
    {
        return await _orderRepository.DeleteAsync(id);
    }

    private static OrderDto MapToDto(Order order)
    {
        return new OrderDto(
            order.Id,
            order.UserId,
            order.ProductId,
            order.UnitPrice,
            order.Quantity,
            order.TotalPrice,
            order.Status,
            order.CreatedAt,
            order.UpdatedAt
        );
    }
    private static bool IsValidStatusTransition(OrderStatus currentStatus, OrderStatus newStatus)
    {
        return (currentStatus, newStatus) switch
        {
            (OrderStatus.Pending, OrderStatus.Shipping) => true,
            (OrderStatus.Pending, OrderStatus.Cancelled) => true,
            (OrderStatus.Shipping, OrderStatus.Delivered) => true,
            (OrderStatus.Shipping, OrderStatus.Cancelled) => true,
            _ => false
        };
    }
}