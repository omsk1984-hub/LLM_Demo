namespace LLM_Demo.Domain.Conversations;

public sealed class Conversation
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public ConversationStatus Status { get; set; } = ConversationStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
