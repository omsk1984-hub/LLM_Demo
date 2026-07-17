namespace LLM_Demo.Infrastructure.Scheduling;

using LLM_Demo.Domain.Tools;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl.Matchers;

/// <summary>
/// Service for scheduling agent and tool jobs via Quartz.NET.
/// </summary>
public sealed class QuartzService
{
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly ILogger<QuartzService> _logger;

    public QuartzService(ISchedulerFactory schedulerFactory, ILogger<QuartzService> logger)
    {
        _schedulerFactory = schedulerFactory;
        _logger = logger;
    }

    public static ToolDefinition Definition => new()
    {
        Name = "quartz_scheduler",
        Description = "Schedules tasks to run at specific times using cron expressions. " +
                      "Actions: schedule_agent, schedule_tool, list_jobs, unschedule."
    };

    public async Task<ToolResult> ExecuteAsync(string action, string jobName, string cronExpression,
        string? agentId = null, string? toolName = null)
    {
        var scheduler = await _schedulerFactory.GetScheduler();

        return action.ToLowerInvariant() switch
        {
            "schedule_agent" => await ScheduleAgentJobAsync(scheduler, jobName, cronExpression, agentId),
            "schedule_tool" => await ScheduleToolJobAsync(scheduler, jobName, cronExpression, toolName),
            "list_jobs" => await ListJobsAsync(scheduler),
            "unschedule" => await UnscheduleJobAsync(scheduler, jobName),
            _ => ToolResult.Failure($"Unknown action '{action}'. Use: schedule_agent, schedule_tool, list_jobs, unschedule")
        };
    }

    private async Task<ToolResult> ScheduleAgentJobAsync(IScheduler scheduler, string jobName,
        string cronExpression, string? agentId)
    {
        if (string.IsNullOrEmpty(agentId))
            return ToolResult.Failure("agentId is required for schedule_agent");

        var job = JobBuilder.Create<AgentJob>()
            .WithIdentity(jobName)
            .UsingJobData("AgentId", agentId)
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity($"{jobName}-trigger")
            .WithCronSchedule(cronExpression)
            .Build();

        await scheduler.ScheduleJob(job, trigger);
        _logger.LogInformation("Scheduled agent job '{JobName}' for agent {AgentId} with cron '{Cron}'",
            jobName, agentId, cronExpression);

        return ToolResult.Success($"Agent job '{jobName}' scheduled with cron '{cronExpression}'");
    }

    private async Task<ToolResult> ScheduleToolJobAsync(IScheduler scheduler, string jobName,
        string cronExpression, string? toolName)
    {
        if (string.IsNullOrEmpty(toolName))
            return ToolResult.Failure("toolName is required for schedule_tool");

        var job = JobBuilder.Create<ToolJob>()
            .WithIdentity(jobName)
            .UsingJobData("ToolName", toolName)
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity($"{jobName}-trigger")
            .WithCronSchedule(cronExpression)
            .Build();

        await scheduler.ScheduleJob(job, trigger);
        _logger.LogInformation("Scheduled tool job '{JobName}' for tool {ToolName} with cron '{Cron}'",
            jobName, toolName, cronExpression);

        return ToolResult.Success($"Tool job '{jobName}' scheduled with cron '{cronExpression}'");
    }

    private async Task<ToolResult> ListJobsAsync(IScheduler scheduler)
    {
        var jobGroups = await scheduler.GetJobGroupNames();
        var jobs = new List<string>();

        foreach (var group in jobGroups)
        {
            var groupJobs = await scheduler.GetJobKeys(GroupMatcher<JobKey>.GroupEquals(group));
            foreach (var jobKey in groupJobs)
            {
                var triggers = await scheduler.GetTriggersOfJob(jobKey);
                foreach (var trigger in triggers)
                {
                    var nextFire = trigger.GetNextFireTimeUtc()?.LocalDateTime.ToString("g") ?? "N/A";
                    jobs.Add($"Job: {jobKey.Name}, Group: {group}, Next: {nextFire}");
                }
            }
        }

        return ToolResult.Success(string.Join("\n", jobs));
    }

    private async Task<ToolResult> UnscheduleJobAsync(IScheduler scheduler, string jobName)
    {
        var jobKey = new JobKey(jobName);
        if (!await scheduler.CheckExists(jobKey))
            return ToolResult.Failure($"Job '{jobName}' not found");

        await scheduler.DeleteJob(jobKey);
        _logger.LogInformation("Unscheduled job '{JobName}'", jobName);

        return ToolResult.Success($"Job '{jobName}' unscheduled");
    }
}
