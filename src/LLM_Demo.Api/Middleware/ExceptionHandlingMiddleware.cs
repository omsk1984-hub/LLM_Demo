namespace LLM_Demo.Api.Middleware;

using System.Net;
using System.Text.Json;
using LLM_Demo.Api.Models.Responses;

public sealed class ExceptionHandlingMiddleware
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
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            await WriteErrorAsync(context, "Forbidden", ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found");
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            await WriteErrorAsync(context, "Not Found", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            await WriteErrorAsync(context, "Internal Server Error",
                context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment()
                    ? ex.Message
                    : "An unexpected error occurred.");
        }
    }

    private static async Task WriteErrorAsync(HttpContext context, string error, string detail)
    {
        context.Response.ContentType = "application/json";
        var response = new ErrorResponse(error, detail);
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
