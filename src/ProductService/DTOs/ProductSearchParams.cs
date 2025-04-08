namespace ProductService.DTOs;

public record ProductSearchParams(
    string? SearchTerm,
    string? Category,
    decimal? MinPrice,
    decimal? MaxPrice,
    string? SortBy,
    bool SortDescending,
    int Page = 1,
    int PageSize = 10
);