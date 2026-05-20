using InventorySystem.Application.DTOs.Stock;

namespace InventorySystem.Application.Interfaces;

public interface IStockService
{
    Task<StockResponseDto> GetStockAsync(int combinationId, CancellationToken cancellationToken = default);
    Task<StockResponseDto> AddStockAsync(AddStockDto dto, CancellationToken cancellationToken = default);
    Task<StockResponseDto> RemoveStockAsync(RemoveStockDto dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockResponseDto>> GetAllStockByProductAsync(int productId, CancellationToken cancellationToken = default);
}
