namespace LLM_Demo.Agents.Grains;

using LLM_Demo.Agents.Interfaces;
using LLM_Demo.Domain.Messages;
using Microsoft.Extensions.Logging;

public sealed class ConversationGrain : Grain, IConversationGrain
{
    private readonly ILogger<ConversationGrain> _logger;
    private ConversationState _state = new();

    public ConversationGrain(ILogger<ConversationGrain> logger)
    {
        _logger = logger;
    }

    public Task<ConversationState> GetStateAsync() => Task.FromResult(_state);

    public Task AddMessageAsync(Message message)
    {
        _state.Messages.Add(message);
        _logger.LogDebug("Message added to conversation {ConversationId}: {Role}",
            this.GetPrimaryKey(), message.Role);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Message>> GetMessagesAsync() =>
        Task.FromResult<IReadOnlyList<Message>>(_state.Messages.AsReadOnly());
}
