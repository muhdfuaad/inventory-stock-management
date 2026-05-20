using InventorySystem.Application.Interfaces;
using InventorySystem.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace InventorySystem.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IVariantService, VariantService>();
        services.AddScoped<IStockService, StockService>();

        return services;
    }
}
