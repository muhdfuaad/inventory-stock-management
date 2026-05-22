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

        var variant = product.Variants
            .FirstOrDefault(v => string.Equals(v.Name, name, StringComparison.OrdinalIgnoreCase));

        if (variant is null)
        {
            variant = new ProductVariant
            {
                ProductId = productId,
                Name = name,
                Values = values
            };

            await dbContext.ProductVariants.AddAsync(variant, cancellationToken);
        }
        else
        {
            var existingValues = variant.Values.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var newValues = values
                .Where(value => !existingValues.Contains(value))
                .ToList();

            if (newValues.Count == 0)
            {
                logger.LogWarning("No new values supplied for variant {VariantName} on product {ProductId}", name, productId);
                throw new ValidationException($"Variant '{name}' already contains the supplied values.");
            }

            variant.Values = variant.Values
                .Concat(newValues)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var allVariants = await dbContext.ProductVariants
            .Where(v => v.ProductId == productId)
            .ToListAsync(cancellationToken);

        await SynchronizeCombinationsAsync(product, allVariants, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation("Variant {VariantName} updated for product {ProductId}", name, productId);

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

        dbContext.ProductVariants.Remove(variant);
        await dbContext.SaveChangesAsync(cancellationToken);

        var remainingVariants = await dbContext.ProductVariants
            .Where(v => v.ProductId == productId)
            .ToListAsync(cancellationToken);

        await SynchronizeCombinationsAsync(product, remainingVariants, cancellationToken);

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

    private async Task SynchronizeCombinationsAsync(
        Product product,
        IReadOnlyCollection<ProductVariant> currentVariants,
        CancellationToken cancellationToken)
    {
        var generatedCombinations = CombinationGenerator.Generate(currentVariants);
        var generatedByKey = generatedCombinations.ToDictionary(
            combination => combination.CombinationKey,
            StringComparer.OrdinalIgnoreCase);
        var generatedKeys = generatedByKey.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var existingCombinations = await dbContext.ProductVariantCombinations
            .Include(c => c.Stock)
            .Where(c => c.ProductId == product.Id)
            .ToListAsync(cancellationToken);

        var existingKeys = existingCombinations
            .Select(c => c.CombinationKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var combinationsToDelete = existingCombinations
            .Where(c => !generatedKeys.Contains(c.CombinationKey))
            .ToList();

        var stockToDelete = combinationsToDelete
            .Where(c => c.Stock is not null)
            .Select(c => c.Stock)
            .ToList();

        dbContext.Stocks.RemoveRange(stockToDelete);
        dbContext.ProductVariantCombinations.RemoveRange(combinationsToDelete);

        var defaultPrice = existingCombinations
            .FirstOrDefault(c => generatedKeys.Contains(c.CombinationKey))?.Price
            ?? existingCombinations.FirstOrDefault()?.Price
            ?? 0m;

        var keysToAdd = generatedKeys
            .Where(key => !existingKeys.Contains(key))
            .ToList();

        foreach (var key in keysToAdd)
        {
            var generatedCombination = generatedByKey[key];

            await dbContext.ProductVariantCombinations.AddAsync(new ProductVariantCombination
            {
                ProductId = product.Id,
                CombinationKey = generatedCombination.CombinationKey,
                DisplayLabel = generatedCombination.DisplayLabel,
                RawCombination = generatedCombination.RawCombination,
                Price = defaultPrice,
                SKU = BuildSku(product.Name, generatedCombination.CombinationKey),
                Stock = new Stock { Quantity = 0 }
            }, cancellationToken);
        }
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
