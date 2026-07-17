namespace LLM_Demo.Application.SubAgents;

using LLM_Demo.Domain.Agents;

public interface ISubAgentRouter
{
    IReadOnlyList<SubAgentReference> RouteTask(string task, IReadOnlyList<SubAgentReference> availableSubAgents);
}

/// <summary>
/// Default router that returns all available sub-agents.
/// Override with custom routing logic (e.g., based on task keywords).
/// </summary>
public sealed class DefaultSubAgentRouter : ISubAgentRouter
{
    public IReadOnlyList<SubAgentReference> RouteTask(
        string task,
        IReadOnlyList<SubAgentReference> availableSubAgents)
    {
        // Default: return all sub-agents
        // In production: parse task keywords, match agent descriptions
        return availableSubAgents;
    }
}
