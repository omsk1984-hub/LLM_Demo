namespace LLM_Demo.Domain.Tools;

public sealed class ToolDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, string> Parameters { get; set; } = [];
}
