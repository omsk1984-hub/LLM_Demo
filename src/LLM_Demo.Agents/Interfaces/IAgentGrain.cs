namespace LLM_Demo.Agents.Interfaces;

using LLM_Demo.Domain.Messages;

public interface IAgentGrain : Orleans.IGrainWithGuidKey
{
    Task<AgentState> GetStateAsync();
    Task SetStateAsync(AgentState state);
    Task<StreamingChunk> ExecuteTaskAsync(string task);
    Task<string> GetStatusAsync();
}

[Serializable]
public sealed record AgentState
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "Idle";
    public string SystemPrompt { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
}
