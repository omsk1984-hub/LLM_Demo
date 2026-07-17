namespace LLM_Demo.Domain.Agents;

using LLM_Demo.Domain.Conversations;
using LLM_Demo.Domain.Messages;

public interface IAgentLoop
{
    Task<AgentLoopResult> ExecuteAsync(
        Conversation conversation,
        Agent agent,
        CancellationToken ct = default);

    IAsyncEnumerable<StreamingChunk> ExecuteStreamingAsync(
        Conversation conversation,
        Agent agent,
        CancellationToken ct = default);
}
