using MongoDB.Driver;
using MongoDB.Driver.Linq;
using ProductService.Models;
using ProductService.DTOs;
using ProductService.Repositories;

namespace ProductService.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly IMongoCollection<Product> _products;
    private readonly ILogger<ProductRepository> _logger;	

    public ProductRepository(IMongoDatabase database, ILogger<ProductRepository> logger)
    {
        _products = database.GetCollection<Product>("products");
        _logger = logger;
    
    // Create indexes
    var indexKeysBuilder = Builders<Product>.IndexKeys;
    var indexModels = new List<CreateIndexModel<Product>>
    {
        new(indexKeysBuilder.Ascending(p => p.Name)),
        new(indexKeysBuilder.Ascending(p => p.Category)),
        new(indexKeysBuilder.Ascending(p => p.Price))
    };

    _products.Indexes.CreateManyAsync(indexModels);
    _logger.LogInformation("Indexes created for Product collection.");
    }
    public async Task<Product> CreateAsync(Product product)
    {
        await _products.InsertOneAsync(product);
        return product;
    }
    public async Task<Product?> GetByIdAsync(string id)
    {
        return await _products.Find(p => p.Id == id).FirstOrDefaultAsync();
    }
    public async Task<(IEnumerable<Product> Products, int TotalCount)> SearchAsync(ProductSearchParams searchParams)
    {
        var builder = Builders<Product>.Filter;
        var filter = builder.Empty;

        if (!string.IsNullOrWhiteSpace(searchParams.SearchTerm))
        {
            filter &= builder.Or(
                builder.Regex(p => p.Name, new MongoDB.Bson.BsonRegularExpression(searchParams.SearchTerm, "i")),
                builder.Regex(p => p.Description, new MongoDB.Bson.BsonRegularExpression(searchParams.SearchTerm, "i"))
            );
        }
        if (!string.IsNullOrWhiteSpace(searchParams.Category))
        {
            filter &= builder.Eq(p => p.Category, searchParams.Category);
        }

        if (searchParams.MinPrice.HasValue)
        {
            filter &= builder.Gte(p => p.Price, searchParams.MinPrice.Value);
        }

        if (searchParams.MaxPrice.HasValue)
        {
            filter &= builder.Lte(p => p.Price, searchParams.MaxPrice.Value);
        }

        var totalCount = await _products.CountDocumentsAsync(filter);

        var sortDefinition = searchParams.SortBy?.ToLower() switch
        {
            "price" => searchParams.SortDescending ? 
                Builders<Product>.Sort.Descending(p => p.Price) : 
                Builders<Product>.Sort.Ascending(p => p.Price),
            "name" => searchParams.SortDescending ? 
                Builders<Product>.Sort.Descending(p => p.Name) : 
                Builders<Product>.Sort.Ascending(p => p.Name),
            _ => Builders<Product>.Sort.Descending(p => p.CreatedAt)
        };

        var products = await _products.Find(filter)
            .Sort(sortDefinition)
            .Skip((searchParams.Page - 1) * searchParams.PageSize)
            .Limit(searchParams.PageSize)
            .ToListAsync();

        return (products, (int)totalCount);
    }

    public async Task<bool> UpdateAsync(string id, UpdateProductDto productDto)
    {
        var update = Builders<Product>.Update;
        var updates = new List<UpdateDefinition<Product>>();

        if (productDto.Name != null)
            updates.Add(update.Set(p => p.Name, productDto.Name));
        if (productDto.Description != null)
            updates.Add(update.Set(p => p.Description, productDto.Description));
        if (productDto.Price.HasValue)
            updates.Add(update.Set(p => p.Price, productDto.Price.Value));
        if (productDto.Category != null)
            updates.Add(update.Set(p => p.Category, productDto.Category));
        if (productDto.StockQuantity.HasValue)
            updates.Add(update.Set(p => p.StockQuantity, productDto.StockQuantity.Value));
        if (productDto.Tags != null)
            updates.Add(update.Set(p => p.Tags, productDto.Tags));

        updates.Add(update.Set(p => p.UpdatedAt, DateTime.UtcNow));

        var result = await _products.UpdateOneAsync(
            p => p.Id == id,
            update.Combine(updates)
        );

        return result.ModifiedCount == 1;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var result = await _products.DeleteOneAsync(p => p.Id == id);
        return result.DeletedCount == 1;
    }

    public async Task<bool> ExistsAsync(string id)
    {
        return await _products.Find(p => p.Id == id).AnyAsync();
    }
}