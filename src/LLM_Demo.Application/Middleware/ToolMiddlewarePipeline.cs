namespace LLM_Demo.Application.Middleware;

using LLM_Demo.Domain.Middleware;
using LLM_Demo.Domain.Tools;
using Microsoft.Extensions.Logging;

public sealed class ToolMiddlewarePipeline
{
    private readonly List<IToolMiddleware> _middleware;
    private readonly Func<ToolMiddlewareContext, Task<ToolResult>> _coreHandler;
    private readonly ILogger<ToolMiddlewarePipeline> _logger;

    public ToolMiddlewarePipeline(
        IEnumerable<IToolMiddleware> middleware,
        Func<ToolMiddlewareContext, Task<ToolResult>> coreHandler,
        ILogger<ToolMiddlewarePipeline> logger)
    {
        _middleware = middleware.ToList();
        _coreHandler = coreHandler;
        _logger = logger;
    }

    public async Task<ToolResult> ExecuteAsync(ToolMiddlewareContext context)
    {
        _logger.LogDebug("Executing tool '{ToolName}' through pipeline with {Count} middleware",
            context.ToolCall.Name, _middleware.Count);

        // Build the chain: middleware[0] → middleware[1] → ... → coreHandler
        Func<ToolMiddlewareContext, Task<ToolResult>> pipeline = _coreHandler;

        for (var i = _middleware.Count - 1; i >= 0; i--)
        {
            var current = _middleware[i];
            var next = pipeline;
            pipeline = ctx => current.ExecuteAsync(ctx, next);
        }

        return await pipeline(context);
    }
}
