using OrderService.Enums;

namespace OrderService.Models;

public class Order
{
    public int Id { get; set; }
    public string UserId { get; set; } = null!;
    public string ProductId { get; set; } = null!;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}