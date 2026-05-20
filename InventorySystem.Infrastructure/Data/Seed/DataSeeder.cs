using InventorySystem.Domain.Entities;
using InventorySystem.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;

namespace InventorySystem.Infrastructure.Data.Seed;

public static class DataSeeder
{
    private const int InitialStockQuantity = 50;

    public static async Task SeedAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (await dbContext.Products.AnyAsync(cancellationToken))
        {
            return;
        }

        var products = new[]
        {
            CreateProduct(
                name: "Shirt",
                description: "Cotton casual shirt",
                basePrice: 29.99m,
                variants:
                [
                    new ProductVariant { Name = "Size", Values = ["S", "M", "L", "XL"] },
                    new ProductVariant { Name = "Color", Values = ["Red", "Blue", "White"] }
                ]),
            CreateProduct(
                name: "Shoes",
                description: "Leather formal shoes",
                basePrice: 89.99m,
                variants:
                [
                    new ProductVariant { Name = "Size", Values = ["7", "8", "9", "10"] },
                    new ProductVariant { Name = "Color", Values = ["Black", "Brown"] }
                ]),
            CreateProduct(
                name: "Mobile",
                description: "Android smartphone",
                basePrice: 499.99m,
                variants:
                [
                    new ProductVariant { Name = "RAM", Values = ["6GB", "8GB", "12GB"] },
                    new ProductVariant { Name = "Storage", Values = ["128GB", "256GB"] }
                ])
        };

        await dbContext.Products.AddRangeAsync(products, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static Product CreateProduct(
        string name,
        string description,
        decimal basePrice,
        IEnumerable<ProductVariant> variants)
    {
        var product = new Product
        {
            Name = name,
            Description = description
        };

        foreach (var variant in variants)
        {
            product.Variants.Add(variant);
        }

        foreach (var generatedCombination in CombinationGenerator.Generate(product.Variants))
        {
            var combination = new ProductVariantCombination
            {
                CombinationKey = generatedCombination.CombinationKey,
                DisplayLabel = generatedCombination.DisplayLabel,
                RawCombination = generatedCombination.RawCombination,
                Price = basePrice,
                SKU = BuildSku(name, generatedCombination.CombinationKey),
                Stock = new Stock { Quantity = InitialStockQuantity }
            };

            product.Combinations.Add(combination);
        }

        return product;
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
