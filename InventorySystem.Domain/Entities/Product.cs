using InventorySystem.Domain.Common;

namespace InventorySystem.Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDeleted { get; set; } = false;

    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    public ICollection<ProductVariantCombination> Combinations { get; set; } = new List<ProductVariantCombination>();
}
