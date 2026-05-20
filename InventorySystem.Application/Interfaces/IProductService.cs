using InventorySystem.Application.Common;
using InventorySystem.Application.DTOs.Product;

namespace InventorySystem.Application.Interfaces;

public interface IProductService
{
    Task<PagedResult<ProductResponseDto>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ProductResponseDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ProductResponseDto> CreateAsync(CreateProductDto dto, CancellationToken cancellationToken = default);
    Task<ProductResponseDto> UpdateAsync(int id, UpdateProductDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
