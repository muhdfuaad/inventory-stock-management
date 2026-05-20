using InventorySystem.Application.Common.Exceptions;
using InventorySystem.Application.DTOs.Stock;
using InventorySystem.Application.Interfaces;
using InventorySystem.Domain.Entities;
using InventorySystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InventorySystem.Application.Services;

public class StockService(
    AppDbContext dbContext,
    ILogger<StockService> logger) : IStockService
{
    public async Task<StockResponseDto> GetStockAsync(int combinationId, CancellationToken cancellationToken = default)
    {
        var stock = await dbContext.Stocks
            .AsNoTracking()
            .Include(s => s.Combination)
            .FirstOrDefaultAsync(s => s.CombinationId == combinationId, cancellationToken);

        return stock is null
            ? throw new NotFoundException($"Stock for combination id {combinationId} was not found.")
            : MapStock(stock);
    }

    public async Task<StockResponseDto> AddStockAsync(AddStockDto dto, CancellationToken cancellationToken = default)
    {
        ValidateQuantity(dto.Quantity);

        return await ExecuteWithConcurrencyRetryAsync(
            dto.CombinationId,
            stock =>
            {
                stock.Quantity += dto.Quantity;
                stock.LastUpdated = DateTime.UtcNow;
                logger.LogInformation(
                    "Added {Quantity} stock units to combination {CombinationId}",
                    dto.Quantity,
                    dto.CombinationId);
            },
            cancellationToken);
    }

    public async Task<StockResponseDto> RemoveStockAsync(RemoveStockDto dto, CancellationToken cancellationToken = default)
    {
        ValidateQuantity(dto.Quantity);

        return await ExecuteWithConcurrencyRetryAsync(
            dto.CombinationId,
            stock =>
            {
                if (stock.Quantity < dto.Quantity)
                {
                    logger.LogWarning(
                        "Insufficient stock for combination {CombinationId}. Requested {RequestedQuantity}, available {AvailableQuantity}",
                        dto.CombinationId,
                        dto.Quantity,
                        stock.Quantity);

                    throw new ValidationException($"Only {stock.Quantity} units available");
                }

                stock.Quantity -= dto.Quantity;
                stock.LastUpdated = DateTime.UtcNow;
                logger.LogInformation(
                    "Removed {Quantity} stock units from combination {CombinationId}",
                    dto.Quantity,
                    dto.CombinationId);
            },
            cancellationToken);
    }

    public async Task<IReadOnlyList<StockResponseDto>> GetAllStockByProductAsync(
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

        var stocks = await dbContext.Stocks
            .AsNoTracking()
            .Include(s => s.Combination)
            .Where(s => s.Combination.ProductId == productId)
            .OrderBy(s => s.Combination.CombinationKey)
            .ToListAsync(cancellationToken);

        return stocks.Select(MapStock).ToList();
    }

    private async Task<StockResponseDto> ExecuteWithConcurrencyRetryAsync(
        int combinationId,
        Action<Stock> updateStock,
        CancellationToken cancellationToken)
    {
        const int maxAttempts = 2;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var stock = await dbContext.Stocks
                    .Include(s => s.Combination)
                    .FirstOrDefaultAsync(s => s.CombinationId == combinationId, cancellationToken);

                if (stock is null)
                {
                    throw new NotFoundException($"Stock for combination id {combinationId} was not found.");
                }

                updateStock(stock);

                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return MapStock(stock);
            }
            catch (DbUpdateConcurrencyException) when (attempt < maxAttempts)
            {
                await transaction.RollbackAsync(cancellationToken);
                dbContext.ChangeTracker.Clear();
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync(cancellationToken);
                dbContext.ChangeTracker.Clear();
                throw new ValidationException("Stock was modified by another user. Please retry.");
            }
        }

        throw new ValidationException("Stock was modified by another user. Please retry.");
    }

    private static void ValidateQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ValidationException("Quantity must be greater than zero.");
        }
    }

    private static StockResponseDto MapStock(Stock stock)
    {
        return new StockResponseDto
        {
            CombinationId = stock.CombinationId,
            DisplayLabel = stock.Combination.DisplayLabel,
            CombinationKey = stock.Combination.CombinationKey,
            Quantity = stock.Quantity,
            LastUpdated = stock.LastUpdated
        };
    }
}
