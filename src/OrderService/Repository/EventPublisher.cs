using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using OrderService.Models;
using OrderService.DTOs;

namespace OrderService.Repositories;

public class RabbitMQEventPublisher : IEventPublisher
{
    private readonly IConnection _rabbitConnection;
    private readonly ILogger<RabbitMQEventPublisher> _logger;

    public RabbitMQEventPublisher(IConnection rabbitConnection, ILogger<RabbitMQEventPublisher> logger)
    {
        _rabbitConnection = rabbitConnection;
        _logger = logger;
    }

    public async Task PublishOrderCreatedEventAsync(Order order)
    {
        await PublishEventAsync("orders.created", new
        {
            OrderId = order.Id,
            UserId = order.UserId,
            ProductId = order.ProductId,
            Status = order.Status,
            CreatedAt = order.CreatedAt
        });
    }

    public async Task PublishOrderStatusUpdatedEventAsync(Order order)
    {
        await PublishEventAsync("orders.status-updated", new
        {
            OrderId = order.Id,
            Status = order.Status,
            UpdatedAt = order.UpdatedAt
        });
    }

    private async Task PublishEventAsync<T>(string routingKey, T message)
    {
        try
        {
            using var channel = await _rabbitConnection.CreateChannelAsync();
            await channel.QueueDeclareAsync(
                queue: routingKey,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

            await channel.BasicPublishAsync(
                exchange: "",
                routingKey: routingKey,
                body: body);

            _logger.LogInformation(
                "Event published to queue {QueueName}", 
                routingKey
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error publishing event to queue {QueueName}",
                routingKey
            );
            throw;
        }
    }
}