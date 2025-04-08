namespace ProductService.DTOs;

public record CreateProductDto(
    string Name,
    string Description,
    decimal Price,
    string Category,
    int StockQuantity,
    List<string>? Tags
);