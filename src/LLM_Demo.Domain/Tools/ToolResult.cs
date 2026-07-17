namespace LLM_Demo.Domain.Tools;

public sealed class ToolResult
{
    public bool IsSuccess { get; set; } = true;
    public string? Error { get; set; }
    public string Result { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }

    public static ToolResult Success(string result) => new() { Result = result };
    public static ToolResult Failure(string error) => new() { IsSuccess = false, Error = error };
}
