namespace LLM_Demo.Infrastructure.Scheduling;

using Microsoft.Extensions.Logging;
using Quartz;

/// <summary>
/// Quartz job that triggers an agent execution.
/// </summary>
public sealed class AgentJob : IJob
{
    private readonly ILogger<AgentJob> _logger;

    public AgentJob(ILogger<AgentJob> logger)
    {
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var agentId = context.MergedJobDataMap.GetString("AgentId");
        _logger.LogInformation("Executing scheduled agent job for AgentId: {AgentId}", agentId);

        // In production: resolve AgentGrain and execute
        await Task.CompletedTask;

        _logger.LogInformation("Agent job completed for AgentId: {AgentId}", agentId);
    }
}
