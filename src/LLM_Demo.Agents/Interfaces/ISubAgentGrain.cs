namespace LLM_Demo.Agents.Interfaces;

public interface ISubAgentGrain : Orleans.IGrainWithGuidKey
{
    Task<SubAgentState> GetStateAsync();
    Task ExecuteAsync(string parentTask, string context);
}

[Serializable]
public sealed record SubAgentState
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "Idle";
    public Guid ParentAgentId { get; set; }
}
