using System.ComponentModel.DataAnnotations;

namespace InventorySystem.Blazor.Blazor.Models;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = [];
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class CreateProductDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
}

public class UpdateProductDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
}

public class ProductResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<VariantResponseDto> Variants { get; set; } = [];
    public List<CombinationResponseDto> Combinations { get; set; } = [];
}

public class AddVariantDto
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    public List<string> Values { get; set; } = [];
}

public class UpdateVariantDto
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    public List<string> Values { get; set; } = [];
}

public class VariantResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<string> Values { get; set; } = [];
}

public class CombinationResponseDto
{
    public int Id { get; set; }
    public string CombinationKey { get; set; } = string.Empty;
    public string DisplayLabel { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
}

public class AddStockDto
{
    public int CombinationId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; } = 1;
}

public class RemoveStockDto
{
    public int CombinationId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; } = 1;
}

public class StockResponseDto
{
    public int CombinationId { get; set; }
    public string DisplayLabel { get; set; } = string.Empty;
    public string CombinationKey { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime LastUpdated { get; set; }
}
