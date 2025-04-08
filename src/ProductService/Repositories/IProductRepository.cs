using ProductService.Models;
using ProductService.DTOs;

namespace ProductService.Repositories;

public interface IProductRepository
{
    Task<Product> CreateAsync(Product product);
    Task<Product?> GetByIdAsync(string id);
    Task<(IEnumerable<Product> Products, int TotalCount)> SearchAsync(ProductSearchParams searchParams);
    Task<bool> UpdateAsync(string id, UpdateProductDto productDto);
    Task<bool> DeleteAsync(string id);
    Task<bool> ExistsAsync(string id);
}