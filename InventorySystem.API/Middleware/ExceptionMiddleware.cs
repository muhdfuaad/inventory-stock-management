using System.Net;
using InventorySystem.Application.Common;
using InventorySystem.Application.Common.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace InventorySystem.API.Middleware;

public class ExceptionMiddleware(
    RequestDelegate next,
    ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (NotFoundException ex)
        {
            logger.LogWarning(ex, "Resource not found at {RequestPath}: {Message}", context.Request.Path, ex.Message);
            await WriteErrorResponseAsync(context, HttpStatusCode.NotFound, ex.Message);
        }
        catch (InventorySystem.Application.Common.Exceptions.ValidationException ex)
        {
            logger.LogWarning(ex, "Validation failure at {RequestPath}: {Message}", context.Request.Path, ex.Message);
            await WriteErrorResponseAsync(context, HttpStatusCode.BadRequest, ex.Message);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency conflict at {RequestPath}", context.Request.Path);
            await WriteErrorResponseAsync(context, HttpStatusCode.Conflict, "The resource was modified by another user. Please retry.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception at {RequestPath}", context.Request.Path);
            await WriteErrorResponseAsync(context, HttpStatusCode.InternalServerError, "An unexpected error occurred.");
        }
    }

    private static async Task WriteErrorResponseAsync(HttpContext context, HttpStatusCode statusCode, string message)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(new
        {
            success = false,
            message
        });
    }
}
