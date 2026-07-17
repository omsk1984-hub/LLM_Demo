namespace LLM_Demo.Application.Middleware;

using LLM_Demo.Domain.Middleware;
using LLM_Demo.Domain.Tools;
using Microsoft.Extensions.Logging;

/// <summary>
/// Safety middleware that filters tool arguments for potentially harmful content.
/// Acts as a "send-safety" gate before tool execution.
/// </summary>
public sealed class SafetyMiddleware : IToolMiddleware
{
    private readonly ILogger<SafetyMiddleware> _logger;

    // Simple blocked patterns — expand as needed
    private static readonly string[] BlockedPatterns =
    [
        "rm -rf",
        "DROP TABLE",
        "DELETE FROM",
        "sudo",
        "chmod 777"
    ];

    public SafetyMiddleware(ILogger<SafetyMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task<ToolResult> ExecuteAsync(
        ToolMiddlewareContext context,
        Func<ToolMiddlewareContext, Task<ToolResult>> next)
    {
        var arguments = context.ToolCall.Arguments;

        if (!string.IsNullOrEmpty(arguments))
        {
            foreach (var pattern in BlockedPatterns)
            {
                if (arguments.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning(
                        "Safety check blocked tool '{ToolName}' — arguments contain blocked pattern '{Pattern}'",
                        context.ToolCall.Name, pattern);

                    return ToolResult.Failure(
                        $"Safety policy blocked execution: pattern '{pattern}' is not allowed.");
                }
            }
        }

        return await next(context);
    }
}
