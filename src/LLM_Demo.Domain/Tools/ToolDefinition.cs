namespace LLM_Demo.Domain.Tools;

public sealed class ToolDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IReadOnlyDictionary<string, Type> Parameters { get; set; } = new Dictionary<string, Type>();
}
