using ProductService.Models;
using ProductService.DTOs;
using ProductService.Repositories;

namespace ProductService.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly ILogger<ProductService> _logger;

    public ProductService(IProductRepository productRepository, ILogger<ProductService> logger)
    {
        _productRepository = productRepository;
        _logger = logger;
    }

    public async Task<ProductDto> CreateAsync(CreateProductDto dto)
    {
        try
        {
            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                Category = dto.Category,
                StockQuantity = dto.StockQuantity,
                Tags = dto.Tags ?? new List<string>()
            };

            var created = await _productRepository.CreateAsync(product);
            return MapToDto(created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product: {ProductName}", dto.Name);
            throw;
        }
    }

    public async Task<ProductDto?> GetByIdAsync(string id)
    {
        try
        {
            var product = await _productRepository.GetByIdAsync(id);
            return product != null ? MapToDto(product) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product: {ProductId}", id);
            throw;
        }
    }

    public async Task<(IEnumerable<ProductDto> Products, int TotalCount)> SearchAsync(ProductSearchParams searchParams)
    {
        try
        {
            var (products, totalCount) = await _productRepository.SearchAsync(searchParams);
            var productDtos = products.Select(MapToDto);
            return (productDtos, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching products");
            throw;
        }
    }

    public async Task<bool> UpdateAsync(string id, UpdateProductDto dto)
    {
        try
        {
            return await _productRepository.UpdateAsync(id, dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product: {ProductId}", id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(string id)
    {
        try
        {
            return await _productRepository.DeleteAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product: {ProductId}", id);
            throw;
        }
    }

    private static ProductDto MapToDto(Product product) => new(
        product.Id,
        product.Name,
        product.Description,
        product.Price,
        product.Category,
        product.StockQuantity,
        product.Tags,
        product.CreatedAt,
        product.UpdatedAt
    );
}