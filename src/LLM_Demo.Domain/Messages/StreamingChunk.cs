namespace LLM_Demo.Domain.Messages;

public sealed class StreamingChunk
{
    public string Content { get; set; } = string.Empty;
    public bool IsFinal { get; set; }
    public string? ToolCallId { get; set; }
    public string? Error { get; set; }
}
