namespace LLM_Demo.Application.Middleware;

using LLM_Demo.Domain.Middleware;
using LLM_Demo.Domain.Tools;
using Microsoft.Extensions.Logging;

public sealed class LoggingMiddleware : IToolMiddleware
{
    private readonly ILogger<LoggingMiddleware> _logger;

    public LoggingMiddleware(ILogger<LoggingMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task<ToolResult> ExecuteAsync(
        ToolMiddlewareContext context,
        Func<ToolMiddlewareContext, Task<ToolResult>> next)
    {
        _logger.LogInformation(
            "Tool call: {ToolName}({Arguments}) by agent '{AgentName}'",
            context.ToolCall.Name,
            context.ToolCall.Arguments,
            context.Agent?.Name ?? "unknown");

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var result = await next(context);
            stopwatch.Stop();

            _logger.LogInformation(
                "Tool '{ToolName}' completed in {DurationMs}ms with success={IsSuccess}",
                context.ToolCall.Name,
                stopwatch.ElapsedMilliseconds,
                result.IsSuccess);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Tool '{ToolName}' failed after {DurationMs}ms",
                context.ToolCall.Name, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
