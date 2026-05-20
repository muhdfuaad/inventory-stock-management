using InventorySystem.Application.DTOs.Variant;

namespace InventorySystem.Application.DTOs.Product;

public class ProductResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public IReadOnlyList<VariantResponseDto> Variants { get; set; } = [];
    public IReadOnlyList<CombinationResponseDto> Combinations { get; set; } = [];
}
