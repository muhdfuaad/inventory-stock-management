using InventorySystem.Application.DTOs.Variant;

namespace InventorySystem.Application.Interfaces;

public interface IVariantService
{
    Task<VariantResponseDto> AddVariantAsync(int productId, AddVariantDto dto, CancellationToken cancellationToken = default);
    Task DeleteVariantAsync(int variantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VariantResponseDto>> GetVariantsByProductAsync(int productId, CancellationToken cancellationToken = default);
}
