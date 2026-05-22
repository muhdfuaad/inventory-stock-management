using System.ComponentModel.DataAnnotations;

namespace InventorySystem.Application.DTOs.Variant;

public class UpdateVariantDto
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    public List<string> Values { get; set; } = [];
}
