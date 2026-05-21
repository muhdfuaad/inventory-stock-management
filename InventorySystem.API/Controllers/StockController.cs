using InventorySystem.Application.Common;
using InventorySystem.Application.DTOs.Stock;
using InventorySystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace InventorySystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StockController(IStockService stockService) : ControllerBase
{
    [HttpGet("{combinationId:int}")]
    public async Task<ActionResult<ApiResponse<StockResponseDto>>> GetStock(
        int combinationId,
        CancellationToken cancellationToken)
    {
        var result = await stockService.GetStockAsync(combinationId, cancellationToken);
        return Ok(ApiResponse<StockResponseDto>.Ok(result));
    }

    [HttpGet("product/{productId:int}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<StockResponseDto>>>> GetByProduct(
        int productId,
        CancellationToken cancellationToken)
    {
        var result = await stockService.GetAllStockByProductAsync(productId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<StockResponseDto>>.Ok(result));
    }

    [HttpPost("add")]
    public async Task<ActionResult<ApiResponse<StockResponseDto>>> AddStock(
        [FromBody] AddStockDto dto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<StockResponseDto>.Fail("Validation failed", GetModelErrors()));
        }

        var result = await stockService.AddStockAsync(dto, cancellationToken);
        return Ok(ApiResponse<StockResponseDto>.Ok(result, "Stock added successfully"));
    }

    [HttpPost("remove")]
    public async Task<ActionResult<ApiResponse<StockResponseDto>>> RemoveStock(
        [FromBody] RemoveStockDto dto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<StockResponseDto>.Fail("Validation failed", GetModelErrors()));
        }

        var result = await stockService.RemoveStockAsync(dto, cancellationToken);
        return Ok(ApiResponse<StockResponseDto>.Ok(result, "Stock removed successfully"));
    }

    private IReadOnlyList<string> GetModelErrors()
    {
        return ModelState.Values
            .SelectMany(value => value.Errors)
            .Select(error => error.ErrorMessage)
            .ToList();
    }
}
