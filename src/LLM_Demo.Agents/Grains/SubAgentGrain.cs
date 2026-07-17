namespace LLM_Demo.Agents.Grains;

using LLM_Demo.Agents.Interfaces;
using Microsoft.Extensions.Logging;

public sealed class SubAgentGrain : Grain, ISubAgentGrain
{
    private readonly ILogger<SubAgentGrain> _logger;
    private SubAgentState _state = new();

    public SubAgentGrain(ILogger<SubAgentGrain> logger)
    {
        _logger = logger;
    }

    public Task<SubAgentState> GetStateAsync() => Task.FromResult(_state);

    public async Task ExecuteAsync(string parentTask, string context)
    {
        _state = _state with { Status = "Running" };
        _logger.LogInformation("Sub-agent '{SubAgentName}' executing: {Task}", _state.Name, parentTask);

        await Task.Delay(100);

        _state = _state with { Status = "Idle" };
        _logger.LogInformation("Sub-agent '{SubAgentName}' completed", _state.Name);
    }
}
