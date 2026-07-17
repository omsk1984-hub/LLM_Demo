namespace LLM_Demo.Infrastructure.Tools;

using System.Data;
using LLM_Demo.Domain.Tools;

/// <summary>
/// Calculator tool that evaluates mathematical expressions.
/// </summary>
public sealed class CalculatorTool
{
    public static ToolDefinition Definition => new()
    {
        Name = "calculator",
        Description = "Evaluates a mathematical expression and returns the result. " +
                      "Supports +, -, *, /, parentheses, and trigonometric functions."
    };

    public ToolResult Execute(string expression)
    {
        try
        {
            var table = new DataTable();
            var result = table.Compute(expression, string.Empty);
            return ToolResult.Success($"{expression} = {result}");
        }
        catch (Exception ex)
        {
            return ToolResult.Failure($"Failed to evaluate '{expression}': {ex.Message}");
        }
    }
}
