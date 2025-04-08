using Microsoft.AspNetCore.Mvc;
using ProductService.Services;
using ProductService.DTOs;

namespace ProductService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IProductService productService, ILogger<ProductsController> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductDto dto)
    {
        try
        {
            var product = await _productService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product");
            return StatusCode(500, "An error occurred while creating the product");
        }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProduct(string id)
    {
        try
        {
            var product = await _productService.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return Ok(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product {ProductId}", id);
            return StatusCode(500, "An error occurred while retrieving the product");
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<ProductDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchProducts([FromQuery] ProductSearchParams searchParams)
    {
        try
        {
            var (products, totalCount) = await _productService.SearchAsync(searchParams);
            var result = new PaginatedResult<ProductDto>
            {
                Items = products,
                TotalCount = totalCount,
                Page = searchParams.Page,
                PageSize = searchParams.PageSize
            };
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching products");
            return StatusCode(500, "An error occurred while searching products");
        }
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProduct(string id, [FromBody] UpdateProductDto dto)
    {
        try
        {
            var success = await _productService.UpdateAsync(id, dto);
            if (!success)
            {
                return NotFound();
            }
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product {ProductId}", id);
            return StatusCode(500, "An error occurred while updating the product");
        }
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduct(string id)
    {
        try
        {
            var success = await _productService.DeleteAsync(id);
            if (!success)
            {
                return NotFound();
            }
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product {ProductId}", id);
            return StatusCode(500, "An error occurred while deleting the product");
        }
    }
}