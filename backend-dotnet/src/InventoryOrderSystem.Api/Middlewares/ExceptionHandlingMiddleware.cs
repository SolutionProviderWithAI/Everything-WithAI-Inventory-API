using System.Net;
using System.Text.Json;
using InventoryOrderSystem.Core.Exceptions;

namespace InventoryOrderSystem.Api.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred during request execution: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";

        var (statusCode, title, detail, extensions) = exception switch
        {
            NotFoundException notFound => (
                HttpStatusCode.NotFound,
                "Resource Not Found",
                notFound.Message,
                null),

            InsufficientStockException stockEx => (
                HttpStatusCode.BadRequest,
                "Insufficient Stock",
                stockEx.Message,
                new Dictionary<string, object>
                {
                    ["productId"] = stockEx.ProductId,
                    ["sku"] = stockEx.Sku,
                    ["requestedQuantity"] = stockEx.RequestedQuantity,
                    ["availableQuantity"] = stockEx.AvailableQuantity
                }),

            BusinessRuleValidationException validationEx => (
                HttpStatusCode.BadRequest,
                "Business Rule Validation Error",
                validationEx.Message,
                null),

            DuplicateOrderException duplicateEx => (
                HttpStatusCode.Conflict,
                "Duplicate Order",
                duplicateEx.Message,
                new Dictionary<string, object>
                {
                    ["idempotencyKey"] = duplicateEx.IdempotencyKey,
                    ["orderNumber"] = duplicateEx.OrderNumber
                }),

            ConcurrencyException concurrencyEx => (
                HttpStatusCode.Conflict,
                "Concurrency Conflict",
                concurrencyEx.Message,
                null),

            _ => (
                HttpStatusCode.InternalServerError,
                "Internal Server Error",
                "An unexpected server error occurred. Please try again later.",
                null)
        };

        context.Response.StatusCode = (int)statusCode;

        var problemDetails = new
        {
            status = (int)statusCode,
            title,
            detail,
            instance = context.Request.Path.Value,
            timestamp = DateTime.UtcNow,
            extensions
        };

        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        await context.Response.WriteAsync(json);
    }
}
