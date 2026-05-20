using InventorySystem.Application.Common.Exceptions;
using InventorySystem.Application.DTOs.Variant;
using InventorySystem.Application.Interfaces;
using InventorySystem.Domain.Entities;
using InventorySystem.Infrastructure.Data;
using InventorySystem.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InventorySystem.Application.Services;

public class VariantService(
    AppDbContext dbContext,
    ILogger<VariantService> logger) : IVariantService
{
    public async Task<VariantResponseDto> AddVariantAsync(
        int productId,
        AddVariantDto dto,
        CancellationToken cancellationToken = default)
    {
        var name = NormalizeVariantName(dto.Name);
        var values = NormalizeValues(dto.Values);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var product = await dbContext.Products
            .Include(p => p.Variants)
            .Include(p => p.Combinations)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

        if (product is null)
        {
            throw new NotFoundException($"Product with id {productId} was not found.");
        }

        if (product.Variants.Any(v => string.Equals(v.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            logger.LogWarning("Duplicate variant {VariantName} attempted for product {ProductId}", name, productId);
            throw new ValidationException($"Variant '{name}' already exists for this product.");
        }

        var variant = new ProductVariant
        {
            ProductId = productId,
            Name = name,
            Values = values
        };

        await dbContext.ProductVariants.AddAsync(variant, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var allVariants = await dbContext.ProductVariants
            .Where(v => v.ProductId == productId)
            .ToListAsync(cancellationToken);

        var existingCombinations = await dbContext.ProductVariantCombinations
            .Where(c => c.ProductId == productId)
            .ToListAsync(cancellationToken);

        var existingKeys = existingCombinations
            .Select(c => c.CombinationKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var defaultPrice = existingCombinations.FirstOrDefault()?.Price ?? 0m;
        var generatedCombinations = CombinationGenerator.Generate(allVariants);

        foreach (var generatedCombination in generatedCombinations.Where(c => !existingKeys.Contains(c.CombinationKey)))
        {
            await dbContext.ProductVariantCombinations.AddAsync(new ProductVariantCombination
            {
                ProductId = productId,
                CombinationKey = generatedCombination.CombinationKey,
                DisplayLabel = generatedCombination.DisplayLabel,
                RawCombination = generatedCombination.RawCombination,
                Price = defaultPrice,
                SKU = BuildSku(product.Name, generatedCombination.CombinationKey),
                Stock = new Stock { Quantity = 0 }
            }, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation("Variant {VariantName} added to product {ProductId}", name, productId);

        return MapVariant(variant);
    }

    public async Task DeleteVariantAsync(int variantId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var variant = await dbContext.ProductVariants
            .FirstOrDefaultAsync(v => v.Id == variantId, cancellationToken);

        if (variant is null)
        {
            throw new NotFoundException($"Variant with id {variantId} was not found.");
        }

        var productId = variant.ProductId;

        var product = await dbContext.Products
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

        if (product is null)
        {
            throw new NotFoundException($"Product with id {productId} was not found.");
        }

        var remainingVariants = await dbContext.ProductVariants
            .Where(v => v.ProductId == productId && v.Id != variantId)
            .ToListAsync(cancellationToken);

        var validCombinations = CombinationGenerator.Generate(remainingVariants);
        var validKeys = validCombinations
            .Select(c => c.CombinationKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var existingCombinations = await dbContext.ProductVariantCombinations
            .Include(c => c.Stock)
            .Where(c => c.ProductId == productId)
            .ToListAsync(cancellationToken);

        var existingKeys = existingCombinations
            .Select(c => c.CombinationKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var orphanedCombinations = remainingVariants.Count == 0
            ? existingCombinations
            : existingCombinations
                .Where(c => !validKeys.Contains(c.CombinationKey))
                .ToList();

        var defaultPrice = existingCombinations.FirstOrDefault()?.Price ?? 0m;

        var orphanedStock = orphanedCombinations
            .Where(c => c.Stock is not null)
            .Select(c => c.Stock)
            .ToList();

        dbContext.Stocks.RemoveRange(orphanedStock);
        dbContext.ProductVariantCombinations.RemoveRange(orphanedCombinations);

        foreach (var generatedCombination in validCombinations.Where(c => !existingKeys.Contains(c.CombinationKey)))
        {
            await dbContext.ProductVariantCombinations.AddAsync(new ProductVariantCombination
            {
                ProductId = productId,
                CombinationKey = generatedCombination.CombinationKey,
                DisplayLabel = generatedCombination.DisplayLabel,
                RawCombination = generatedCombination.RawCombination,
                Price = defaultPrice,
                SKU = BuildSku(product.Name, generatedCombination.CombinationKey),
                Stock = new Stock { Quantity = 0 }
            }, cancellationToken);
        }

        dbContext.ProductVariants.Remove(variant);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VariantResponseDto>> GetVariantsByProductAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        var productExists = await dbContext.Products
            .AsNoTracking()
            .AnyAsync(p => p.Id == productId, cancellationToken);

        if (!productExists)
        {
            throw new NotFoundException($"Product with id {productId} was not found.");
        }

        return await dbContext.ProductVariants
            .AsNoTracking()
            .Where(v => v.ProductId == productId)
            .OrderBy(v => v.Name)
            .Select(v => new VariantResponseDto
            {
                Id = v.Id,
                Name = v.Name,
                Values = v.Values
            })
            .ToListAsync(cancellationToken);
    }

    private static string NormalizeVariantName(string name)
    {
        var normalizedName = name.Trim();

        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            throw new ValidationException("Variant name is required.");
        }

        return normalizedName;
    }

    private static List<string> NormalizeValues(IEnumerable<string> values)
    {
        var normalizedValues = values
            .Select(value => value.Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalizedValues.Count == 0)
        {
            throw new ValidationException("At least one variant value is required.");
        }

        return normalizedValues;
    }

    private static VariantResponseDto MapVariant(ProductVariant variant)
    {
        return new VariantResponseDto
        {
            Id = variant.Id,
            Name = variant.Name,
            Values = variant.Values
        };
    }

    private static string BuildSku(string productName, string combinationKey)
    {
        var productPart = productName.Trim().ToUpperInvariant().Replace(" ", "-");
        var combinationPart = combinationKey
            .Replace(":", "-", StringComparison.Ordinal)
            .Replace("|", "-", StringComparison.Ordinal);

        return $"{productPart}-{combinationPart}";
    }
}
