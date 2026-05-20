using InventorySystem.Domain.Common;

namespace InventorySystem.Domain.Entities;

public class ProductVariant : BaseEntity
{
    public int ProductId { get; set; }

    // Example: "Size", "Color"
    public string Name { get; set; } = string.Empty;

    // Example: ["S","M","L"]
    // Stored as JSON in SQL Server
    public List<string> Values { get; set; } = new();

    public Product Product { get; set; } = null!;
}
