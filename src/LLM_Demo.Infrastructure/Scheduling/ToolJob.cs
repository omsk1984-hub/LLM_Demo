namespace LLM_Demo.Infrastructure.Scheduling;

using Microsoft.Extensions.Logging;
using Quartz;

/// <summary>
/// Quartz job that triggers a tool execution on a schedule.
/// </summary>
public sealed class ToolJob : IJob
{
    private readonly ILogger<ToolJob> _logger;

    public ToolJob(ILogger<ToolJob> logger)
    {
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var toolName = context.MergedJobDataMap.GetString("ToolName");
        _logger.LogInformation("Executing scheduled tool job for: {ToolName}", toolName);

        // In production: resolve tool from registry and execute
        await Task.CompletedTask;

        _logger.LogInformation("Tool job completed for: {ToolName}", toolName);
    }
}
