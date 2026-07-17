namespace LLM_Demo.Domain.Agents;

using LLM_Demo.Domain.Conversations;
using LLM_Demo.Domain.Messages;
using LLM_Demo.Domain.Tools;

public sealed class Agent : LLM_Demo.Domain.Ownership.IOwnable
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = string.Empty;
    public AgentStatus Status { get; set; } = AgentStatus.Idle;
    public string OwnerId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<ToolDefinition> Tools { get; set; } = [];
}
