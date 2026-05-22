using InventorySystem.Application.Common;
using InventorySystem.Application.Common.Exceptions;
using InventorySystem.Application.DTOs.Product;
using InventorySystem.Application.DTOs.Variant;
using InventorySystem.Application.Interfaces;
using InventorySystem.Domain.Entities;
using InventorySystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InventorySystem.Application.Services;

public class ProductService(
    AppDbContext dbContext,
    ILogger<ProductService> logger) : IProductService
{
    public async Task<PagedResult<ProductResponseDto>> GetAllAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = dbContext.Products
            .AsNoTracking()
            .Include(p => p.Variants)
            .Include(p => p.Combinations)
                .ThenInclude(c => c.Stock)
            .OrderByDescending(p => p.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var products = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductResponseDto>
        {
            Items = products.Select(MapProduct).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ProductResponseDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await dbContext.Products
            .AsNoTracking()
            .Include(p => p.Variants)
            .Include(p => p.Combinations)
                .ThenInclude(c => c.Stock)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        return product is null
            ? throw new NotFoundException($"Product with id {id} was not found.")
            : MapProduct(product);
    }

    public async Task<ProductResponseDto> CreateAsync(CreateProductDto dto, CancellationToken cancellationToken = default)
    {
        var name = NormalizeRequiredName(dto.Name, "Product name is required.");

        if (await ProductNameExistsAsync(name, excludedProductId: null, cancellationToken))
        {
            logger.LogWarning("Duplicate product create attempt for name {ProductName}", name);
            throw new ValidationException($"Product '{name}' already exists.");
        }

        var product = new Product
        {
            Name = name,
            Description = NormalizeOptionalText(dto.Description)
        };

        await dbContext.Products.AddAsync(product, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Product created with id {ProductId}", product.Id);

        return MapProduct(product);
    }

    public async Task<ProductResponseDto> UpdateAsync(int id, UpdateProductDto dto, CancellationToken cancellationToken = default)
    {
        var product = await dbContext.Products
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product is null)
        {
            throw new NotFoundException($"Product with id {id} was not found.");
        }

        var name = NormalizeRequiredName(dto.Name, "Product name is required.");

        if (await ProductNameExistsAsync(name, excludedProductId: id, cancellationToken))
        {
            logger.LogWarning("Duplicate product update attempt for name {ProductName}", name);
            throw new ValidationException($"Product '{name}' already exists.");
        }

        product.Name = name;
        product.Description = NormalizeOptionalText(dto.Description);

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Product {ProductId} updated", id);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var product = await dbContext.Products
            .Include(p => p.Variants)
            .Include(p => p.Combinations)
                .ThenInclude(c => c.Stock)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product is null)
        {
            throw new NotFoundException($"Product with id {id} was not found.");
        }

        var stocks = product.Combinations
            .Where(c => c.Stock is not null)
            .Select(c => c.Stock)
            .ToList();

        dbContext.Stocks.RemoveRange(stocks);
        dbContext.ProductVariantCombinations.RemoveRange(product.Combinations);
        dbContext.ProductVariants.RemoveRange(product.Variants);

        product.IsDeleted = true;
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation("Product {ProductId} deleted with related variants, combinations, and stock", id);
    }

    private async Task<bool> ProductNameExistsAsync(
        string name,
        int? excludedProductId,
        CancellationToken cancellationToken)
    {
        var normalizedName = name.ToLower();

        return await dbContext.Products
            .AnyAsync(
                p => p.Name.ToLower() == normalizedName
                    && (!excludedProductId.HasValue || p.Id != excludedProductId.Value),
                cancellationToken);
    }

    private static ProductResponseDto MapProduct(Product product)
    {
        return new ProductResponseDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            CreatedAt = product.CreatedAt,
            Variants = product.Variants
                .OrderBy(v => v.Name)
                .Select(v => new VariantResponseDto
                {
                    Id = v.Id,
                    Name = v.Name,
                    Values = v.Values
                })
                .ToList(),
            Combinations = product.Combinations
                .OrderBy(c => c.CombinationKey)
                .Select(c => new CombinationResponseDto
                {
                    Id = c.Id,
                    CombinationKey = c.CombinationKey,
                    DisplayLabel = c.DisplayLabel,
                    CurrentStock = c.Stock?.Quantity ?? 0
                })
                .ToList()
        };
    }

    private static string NormalizeRequiredName(string value, string errorMessage)
    {
        var name = value.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException(errorMessage);
        }

        return name;
    }

    private static string? NormalizeOptionalText(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
