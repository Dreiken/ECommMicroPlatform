using System.ComponentModel.DataAnnotations;
using OrderService.Enums;

namespace OrderService.DTOs;

public record UpdateOrderStatusDto
{
    [Required]
    [EnumDataType(typeof(OrderStatus))]
    public OrderStatus Status { get; init; }

    public string? Reason { get; init; }
}