namespace LLM_Demo.Agents.Grains;

using LLM_Demo.Agents.Interfaces;
using LLM_Demo.Domain.Messages;
using Microsoft.Extensions.Logging;

public sealed class AgentGrain : Grain, IAgentGrain
{
    private readonly ILogger<AgentGrain> _logger;
    private AgentState _state = new();

    public AgentGrain(ILogger<AgentGrain> logger)
    {
        _logger = logger;
    }

    public Task<AgentState> GetStateAsync() => Task.FromResult(_state);

    public Task SetStateAsync(AgentState state)
    {
        _state = state;
        return Task.CompletedTask;
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators
    public async Task<StreamingChunk> ExecuteTaskAsync(string task)
#pragma warning restore CS1998
    {
        _state = _state with { Status = "Running" };

        _logger.LogInformation("Agent '{AgentName}' executing task: {Task}", _state.Name, task);

        var result = new StreamingChunk
        {
            Content = $"Agent '{_state.Name}' processed: {task}",
            IsFinal = true
        };

        _state = _state with { Status = "Idle" };

        return result;
    }

    public Task<string> GetStatusAsync() => Task.FromResult(_state.Status);
}
