using InventorySystem.Application.Common;
using InventorySystem.Application.DTOs.Product;
using InventorySystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace InventorySystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController(IProductService productService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<ProductResponseDto>>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await productService.GetAllAsync(page, pageSize, cancellationToken);
        return Ok(ApiResponse<PagedResult<ProductResponseDto>>.Ok(result));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<ProductResponseDto>>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await productService.GetByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<ProductResponseDto>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ProductResponseDto>>> Create(
        [FromBody] CreateProductDto dto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<ProductResponseDto>.Fail("Validation failed", GetModelErrors()));
        }

        var result = await productService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<ProductResponseDto>.Ok(result, "Product created successfully"));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<ProductResponseDto>>> Update(
        int id,
        [FromBody] UpdateProductDto dto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<ProductResponseDto>.Fail("Validation failed", GetModelErrors()));
        }

        var result = await productService.UpdateAsync(id, dto, cancellationToken);
        return Ok(ApiResponse<ProductResponseDto>.Ok(result, "Product updated successfully"));
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        await productService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, "Product deleted successfully"));
    }

    private IReadOnlyList<string> GetModelErrors()
    {
        return ModelState.Values
            .SelectMany(value => value.Errors)
            .Select(error => error.ErrorMessage)
            .ToList();
    }
}
