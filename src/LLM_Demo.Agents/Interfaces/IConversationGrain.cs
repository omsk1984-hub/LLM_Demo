namespace LLM_Demo.Agents.Interfaces;

using LLM_Demo.Domain.Messages;

public interface IConversationGrain : Orleans.IGrainWithGuidKey
{
    Task<ConversationState> GetStateAsync();
    Task AddMessageAsync(Message message);
    Task<IReadOnlyList<Message>> GetMessagesAsync();
}

[Serializable]
public sealed record ConversationState
{
    public string Title { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public List<Message> Messages { get; set; } = [];
}
