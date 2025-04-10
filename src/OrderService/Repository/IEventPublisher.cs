using OrderService.Models;
using OrderService.DTOs;

namespace OrderService.Repositories;

public interface IEventPublisher
{
    Task PublishOrderCreatedEventAsync(Order order);
    Task PublishOrderStatusUpdatedEventAsync(Order order);
}