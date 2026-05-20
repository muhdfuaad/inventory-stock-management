namespace InventorySystem.Application.DTOs.Variant;

public class CombinationResponseDto
{
    public int Id { get; set; }
    public string CombinationKey { get; set; } = string.Empty;
    public string DisplayLabel { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
}
