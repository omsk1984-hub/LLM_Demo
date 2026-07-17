namespace LLM_Demo.Application.SubAgents;

using LLM_Demo.Domain.Agents;
using LLM_Demo.Domain.Common;
using Microsoft.Extensions.Logging;

public enum SubAgentStrategy
{
    Sequential,
    Parallel,
    Hierarchical
}

public sealed class SubAgentOrchestrator
{
    private readonly ILogger<SubAgentOrchestrator> _logger;

    public SubAgentOrchestrator(ILogger<SubAgentOrchestrator> logger)
    {
        _logger = logger;
    }

    public async Task<Result<string>> ExecuteSubAgentsAsync(
        string task,
        IEnumerable<SubAgentReference> subAgents,
        SubAgentStrategy strategy = SubAgentStrategy.Parallel,
        CancellationToken ct = default)
    {
        var subAgentList = subAgents.ToList();

        if (subAgentList.Count == 0)
        {
            return Result<string>.Success(string.Empty);
        }

        _logger.LogInformation("Executing {Count} sub-agents with {Strategy} strategy",
            subAgentList.Count, strategy);

        var results = new List<string>();

        switch (strategy)
        {
            case SubAgentStrategy.Sequential:
                foreach (var subAgent in subAgentList)
                {
                    var result = await ExecuteSingleSubAgentAsync(subAgent, task, ct);
                    results.Add($"[{subAgent.Name}]: {result}");
                }
                break;

            case SubAgentStrategy.Parallel:
                var tasks = subAgentList.Select(sa => ExecuteSingleSubAgentAsync(sa, task, ct));
                var parallelResults = await Task.WhenAll(tasks);
                results.AddRange(parallelResults.Select((r, i) => $"[{subAgentList[i].Name}]: {r}"));
                break;

            case SubAgentStrategy.Hierarchical:
                // Execute sequentially but each sub-agent sees previous results
                foreach (var subAgent in subAgentList)
                {
                    var enrichedTask = $"{task}\n\nPrevious results:\n{string.Join("\n", results)}";
                    var result = await ExecuteSingleSubAgentAsync(subAgent, enrichedTask, ct);
                    results.Add($"[{subAgent.Name}]: {result}");
                }
                break;
        }

        return Result<string>.Success(string.Join("\n\n", results));
    }

    private Task<string> ExecuteSingleSubAgentAsync(
        SubAgentReference subAgent,
        string task,
        CancellationToken ct)
    {
        // In a full implementation, this would:
        // 1. Resolve the sub-agent grain from Orleans
        // 2. Execute the agent loop for the sub-agent
        // 3. Return the result
        // For now, this is a placeholder that demonstrates the pattern.
        _logger.LogInformation("Executing sub-agent {SubAgentName} ({SubAgentId})",
            subAgent.Name, subAgent.SubAgentId);

        return Task.FromResult($"Sub-agent '{subAgent.Name}' execution placeholder.");
    }
}
