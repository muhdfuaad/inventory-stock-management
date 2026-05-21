using InventorySystem.Application;
using InventorySystem.Application.Common;
using InventorySystem.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;

namespace InventorySystem.API.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddApiServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddApplication();
        services.AddInfrastructure(configuration);
        services.AddControllers()
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context.ModelState.Values
                        .SelectMany(value => value.Errors)
                        .Select(error => error.ErrorMessage)
                        .ToList();

                    return new BadRequestObjectResult(ApiResponse<object>.Fail("Validation failed", errors));
                };
            });
        services.AddEndpointsApiExplorer();

        return services;
    }

    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Inventory System API",
                Version = "v1"
            });
        });

        return services;
    }
}
