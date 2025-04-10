using OrderService.Enums;

namespace OrderService.DTOs;

public record OrderDto(
    int Id,
    string UserId,
    string ProductId,
    decimal UnitPrice,
    int Quantity,
    decimal TotalPrice,
    OrderStatus Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt
)
{
    public string StatusText => Status.ToString();
}