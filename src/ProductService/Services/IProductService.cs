using ProductService.Models;
using ProductService.DTOs;

namespace ProductService.Services;

public interface IProductService
{
    Task<ProductDto> CreateAsync(CreateProductDto dto);
    Task<ProductDto?> GetByIdAsync(string id);
    Task<(IEnumerable<ProductDto> Products, int TotalCount)> SearchAsync(ProductSearchParams searchParams);
    Task<bool> UpdateAsync(string id, UpdateProductDto dto);
    Task<bool> DeleteAsync(string id);
}