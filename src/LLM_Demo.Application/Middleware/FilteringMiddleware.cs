namespace LLM_Demo.Application.Middleware;

using LLM_Demo.Domain.Middleware;
using LLM_Demo.Domain.Tools;
using Microsoft.Extensions.Logging;

public sealed class FilteringMiddleware : IToolMiddleware
{
    private readonly ILogger<FilteringMiddleware> _logger;

    public FilteringMiddleware(ILogger<FilteringMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task<ToolResult> ExecuteAsync(
        ToolMiddlewareContext context,
        Func<ToolMiddlewareContext, Task<ToolResult>> next)
    {
        // Check if the agent has this tool in its allowed list
        if (context.Agent?.Tools is { Count: > 0 })
        {
            var isAllowed = context.Agent.Tools.Any(t =>
                t.Name.Equals(context.ToolCall.Name, StringComparison.OrdinalIgnoreCase));

            if (!isAllowed)
            {
                _logger.LogWarning(
                    "Tool '{ToolName}' is not allowed for agent '{AgentName}'",
                    context.ToolCall.Name, context.Agent.Name);

                return ToolResult.Failure(
                    $"Tool '{context.ToolCall.Name}' is not allowed for this agent.");
            }
        }

        return await next(context);
    }
}
