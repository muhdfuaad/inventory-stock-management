namespace InventorySystem.Application.DTOs.Stock;

public class StockResponseDto
{
    public int CombinationId { get; set; }
    public string DisplayLabel { get; set; } = string.Empty;
    public string CombinationKey { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime LastUpdated { get; set; }
}
