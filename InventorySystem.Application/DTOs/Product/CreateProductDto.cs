using System.ComponentModel.DataAnnotations;

namespace InventorySystem.Application.DTOs.Product;

public class CreateProductDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
}
