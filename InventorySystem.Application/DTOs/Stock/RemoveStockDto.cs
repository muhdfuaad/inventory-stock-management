using System.ComponentModel.DataAnnotations;

namespace InventorySystem.Application.DTOs.Stock;

public class RemoveStockDto
{
    public int CombinationId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}
