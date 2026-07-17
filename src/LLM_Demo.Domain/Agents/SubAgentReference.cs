namespace LLM_Demo.Domain.Agents;

public sealed class SubAgentReference
{
    public Guid Id { get; set; }
    public Guid ParentAgentId { get; set; }
    public Guid SubAgentId { get; set; }
    public string Name { get; set; } = string.Empty;
}
