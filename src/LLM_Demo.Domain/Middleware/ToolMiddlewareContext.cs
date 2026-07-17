namespace LLM_Demo.Domain.Middleware;

using LLM_Demo.Domain.Agents;
using LLM_Demo.Domain.Messages;
using LLM_Demo.Domain.Tools;

public sealed class ToolMiddlewareContext
{
    public ToolCall ToolCall { get; set; } = default!;
    public Message? Message { get; set; }
    public Agent? Agent { get; set; }
    public CancellationToken CancellationToken { get; set; }
}
