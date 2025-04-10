using System.ComponentModel.DataAnnotations;

namespace OrderService.DTOs;

public class CreateOrderDto
{
    [Required]
    public string ProductId { get; init; } = null!;

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
    public int Quantity { get; init; }
}