namespace ProductService.DTOs;

public record ProductDto(
    string Id,
    string Name,
    string Description,
    decimal Price,
    string Category,
    int StockQuantity,
    List<string> Tags,
    DateTime CreatedAt,
    DateTime UpdatedAt
);