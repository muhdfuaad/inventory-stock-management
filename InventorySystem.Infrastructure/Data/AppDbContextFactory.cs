using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.Text.Json;

namespace InventorySystem.Infrastructure.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? TryGetConnectionStringFromSettingsFile()
            ?? "Server=localhost,1433;Database=InventorySystemDb;TrustServerCertificate=True;";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new AppDbContext(options);
    }

    private static string? TryGetConnectionStringFromSettingsFile()
    {
        var currentDirectory = Directory.GetCurrentDirectory();

        var candidateFiles = new[]
        {
            Path.Combine(currentDirectory, "appsettings.Development.json"),
            Path.Combine(currentDirectory, "appsettings.json"),
            Path.Combine(currentDirectory, "InventorySystem.API", "appsettings.Development.json"),
            Path.Combine(currentDirectory, "InventorySystem.API", "appsettings.json")
        };

        foreach (var file in candidateFiles.Where(File.Exists))
        {
            using var stream = File.OpenRead(file);
            using var document = JsonDocument.Parse(stream);

            if (document.RootElement.TryGetProperty("ConnectionStrings", out var connectionStrings)
                && connectionStrings.TryGetProperty("DefaultConnection", out var defaultConnection))
            {
                return defaultConnection.GetString();
            }
        }

        return null;
    }
}
