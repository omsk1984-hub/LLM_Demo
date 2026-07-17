namespace LLM_Demo.Domain.Messages;

public sealed class Message
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public MessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
