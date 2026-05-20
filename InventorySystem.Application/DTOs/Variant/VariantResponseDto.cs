namespace InventorySystem.Application.DTOs.Variant;

public class VariantResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public IReadOnlyList<string> Values { get; set; } = [];
}
