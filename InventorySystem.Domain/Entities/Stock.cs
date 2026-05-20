using InventorySystem.Domain.Common;

namespace InventorySystem.Domain.Entities;

public class Stock : BaseEntity
{
    public int CombinationId { get; set; }

    // NEVER negative
    // Enforced in service layer later
    public int Quantity { get; set; } = 0;

    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    public ProductVariantCombination Combination { get; set; } = null!;
}
