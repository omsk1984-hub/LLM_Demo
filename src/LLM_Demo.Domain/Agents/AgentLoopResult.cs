namespace LLM_Demo.Domain.Agents;

using LLM_Demo.Domain.Messages;
using LLM_Demo.Domain.Common;

public sealed class AgentLoopResult
{
    public bool IsSuccess { get; set; } = true;
    public string? Error { get; set; }
    public List<Message> Messages { get; set; } = [];
    public int Iterations { get; set; }
    public TimeSpan Duration { get; set; }

    public static AgentLoopResult Success(List<Message> messages, int iterations, TimeSpan duration) =>
        new() { Messages = messages, Iterations = iterations, Duration = duration };

    public static AgentLoopResult Failure(string error) =>
        new() { IsSuccess = false, Error = error };
}
