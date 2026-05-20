using System.Text.Json;
using InventorySystem.Domain.Entities;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;

namespace InventorySystem.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductVariantCombination> ProductVariantCombinations => Set<ProductVariantCombination>();
    public DbSet<Stock> Stocks => Set<Stock>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>()
            .Property(p => p.Name)
            .HasMaxLength(100)
            .IsRequired();

        modelBuilder.Entity<Product>()
            .HasIndex(p => p.Name)
            .IsUnique();

        modelBuilder.Entity<Product>()
            .HasQueryFilter(p => !p.IsDeleted);

        modelBuilder.Entity<Product>()
            .Property(p => p.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        modelBuilder.Entity<ProductVariant>()
            .Property(v => v.Values)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null)!)
            .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                (left, right) => left != null && right != null && left.SequenceEqual(right),
                values => values.Aggregate(0, (hash, value) => HashCode.Combine(hash, value.GetHashCode())),
                values => values.ToList()));

        modelBuilder.Entity<ProductVariant>()
            .Property(v => v.Name)
            .HasMaxLength(50)
            .IsRequired();

        modelBuilder.Entity<ProductVariant>()
            .HasIndex(v => new { v.ProductId, v.Name })
            .IsUnique();

        modelBuilder.Entity<ProductVariant>()
            .HasQueryFilter(v => !v.Product.IsDeleted);

        modelBuilder.Entity<ProductVariantCombination>()
            .Property(c => c.CombinationKey)
            .HasMaxLength(300)
            .IsRequired();

        modelBuilder.Entity<ProductVariantCombination>()
            .Property(c => c.DisplayLabel)
            .HasMaxLength(200)
            .IsRequired();

        modelBuilder.Entity<ProductVariantCombination>()
            .HasIndex(c => new { c.ProductId, c.CombinationKey })
            .IsUnique();

        modelBuilder.Entity<ProductVariantCombination>()
            .HasQueryFilter(c => !c.Product.IsDeleted);

        modelBuilder.Entity<ProductVariantCombination>()
            .Property(c => c.Price)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Stock>()
            .Property(s => s.Quantity)
            .IsRequired();

        modelBuilder.Entity<Stock>()
            .Property(s => s.LastUpdated)
            .HasDefaultValueSql("GETUTCDATE()");

        modelBuilder.Entity<Stock>()
            .HasQueryFilter(s => !s.Combination.Product.IsDeleted);

        modelBuilder.Entity<Stock>()
            .HasOne(s => s.Combination)
            .WithOne(c => c.Stock)
            .HasForeignKey<Stock>(s => s.CombinationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Stock>()
            .Property(s => s.RowVersion)
            .IsRowVersion();
    }
}
