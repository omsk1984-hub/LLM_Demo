namespace LLM_Demo.Application.Middleware;

using LLM_Demo.Domain.Messages;
using LLM_Demo.Domain.Middleware;
using LLM_Demo.Domain.Tools;

/// <summary>
/// Middleware that broadcasts streaming chunks to SSE subscribers.
/// Wraps the tool execution result into streaming chunks.
/// </summary>
public sealed class StreamingMiddleware : IToolMiddleware
{
    private readonly StreamingHandler _streamingHandler;

    public StreamingMiddleware(StreamingHandler streamingHandler)
    {
        _streamingHandler = streamingHandler;
    }

    public async Task<ToolResult> ExecuteAsync(
        ToolMiddlewareContext context,
        Func<ToolMiddlewareContext, Task<ToolResult>> next)
    {
        // Notify subscribers that a tool is being called
        await _streamingHandler.BroadcastAsync(new StreamingChunk
        {
            Content = $"[Tool call: {context.ToolCall.Name}]",
            ToolCallId = context.ToolCall.Id
        });

        var result = await next(context);

        // Notify subscribers of the result
        await _streamingHandler.BroadcastAsync(new StreamingChunk
        {
            Content = result.IsSuccess
                ? $"[Tool result: {result.Result[..Math.Min(result.Result.Length, 200)]}]"
                : $"[Tool error: {result.Error}]"
        });

        return result;
    }
}
