namespace LLM_Demo.Domain.Middleware;

using LLM_Demo.Domain.Tools;

public interface IToolMiddleware
{
    Task<ToolResult> ExecuteAsync(
        ToolMiddlewareContext context,
        Func<ToolMiddlewareContext, Task<ToolResult>> next);
}
