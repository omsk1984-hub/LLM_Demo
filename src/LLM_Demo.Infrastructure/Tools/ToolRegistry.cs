namespace LLM_Demo.Infrastructure.Tools;

using LLM_Demo.Domain.Tools;

public interface IToolRegistry
{
    IEnumerable<ToolDefinition> GetAllTools();
    ToolDefinition? GetTool(string name);
}

public sealed class ToolRegistry : IToolRegistry
{
    private readonly Dictionary<string, ToolDefinition> _tools;

    public ToolRegistry(IEnumerable<ToolDefinition> tools)
    {
        _tools = tools.ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);
    }

    public IEnumerable<ToolDefinition> GetAllTools() =>
        _tools.Values;

    public ToolDefinition? GetTool(string name) =>
        _tools.TryGetValue(name, out var tool) ? tool : null;
}
