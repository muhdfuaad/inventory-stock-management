using InventorySystem.Domain.Common;

namespace InventorySystem.Domain.Entities;

public class ProductVariantCombination : BaseEntity
{
    public int ProductId { get; set; }

    // Example:
    // "SIZE:S|COLOR:RED"
    // MUST be unique per product
    public string CombinationKey { get; set; } = string.Empty;

    // Human readable:
    // "S / Red"
    public string DisplayLabel { get; set; } = string.Empty;

    // Optional UI-friendly raw combination
    // Example:
    // "Size:S,Color:Red"
    public string RawCombination { get; set; } = string.Empty;

    // Important for inventory realism
    public decimal Price { get; set; }

    // Optional but good for recruiter impression
    public string SKU { get; set; } = string.Empty;

    public Product Product { get; set; } = null!;
    public Stock Stock { get; set; } = null!;
}
