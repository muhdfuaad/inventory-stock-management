using InventorySystem.Application.Common;
using InventorySystem.Application.DTOs.Variant;
using InventorySystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace InventorySystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VariantsController(IVariantService variantService) : ControllerBase
{
    [HttpGet("/api/products/{productId:int}/variants")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<VariantResponseDto>>>> GetByProduct(
        int productId,
        CancellationToken cancellationToken)
    {
        var result = await variantService.GetVariantsByProductAsync(productId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<VariantResponseDto>>.Ok(result));
    }

    [HttpPost("/api/products/{productId:int}/variants")]
    public async Task<ActionResult<ApiResponse<VariantResponseDto>>> AddVariant(
        int productId,
        [FromBody] AddVariantDto dto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<VariantResponseDto>.Fail("Validation failed", GetModelErrors()));
        }

        var result = await variantService.AddVariantAsync(productId, dto, cancellationToken);
        return Ok(ApiResponse<VariantResponseDto>.Ok(result, "Variant added successfully"));
    }

    [HttpDelete("{variantId:int}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteVariant(
        int variantId,
        CancellationToken cancellationToken)
    {
        await variantService.DeleteVariantAsync(variantId, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, "Variant deleted successfully"));
    }

    [HttpPut("{variantId:int}")]
    public async Task<ActionResult<ApiResponse<VariantResponseDto>>> UpdateVariant(
        int variantId,
        [FromBody] UpdateVariantDto dto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<VariantResponseDto>.Fail("Validation failed", GetModelErrors()));
        }

        var result = await variantService.UpdateVariantAsync(variantId, dto, cancellationToken);
        return Ok(ApiResponse<VariantResponseDto>.Ok(result, "Variant updated successfully"));
    }

    private IReadOnlyList<string> GetModelErrors()
    {
        return ModelState.Values
            .SelectMany(value => value.Errors)
            .Select(error => error.ErrorMessage)
            .ToList();
    }
}
