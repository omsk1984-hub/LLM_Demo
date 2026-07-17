namespace LLM_Demo.Domain.Agents;

public enum AgentStatus
{
    Idle,
    Running,
    WaitingForSubAgent,
    Error
}
