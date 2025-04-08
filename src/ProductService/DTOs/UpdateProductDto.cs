namespace ProductService.DTOs;

public record UpdateProductDto(
    string? Name,
    string? Description,
    decimal? Price,
    string? Category,
    int? StockQuantity,
    List<string>? Tags
);