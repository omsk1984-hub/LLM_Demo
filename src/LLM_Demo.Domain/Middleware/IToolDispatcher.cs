namespace LLM_Demo.Domain.Middleware;

using LLM_Demo.Domain.Tools;

/// <summary>
/// Dispatches tool calls to concrete tool implementations.
/// Implemented in Infrastructure layer where concrete tools reside.
/// </summary>
public interface IToolDispatcher
{
    Task<ToolResult> ExecuteAsync(ToolMiddlewareContext context);
}
