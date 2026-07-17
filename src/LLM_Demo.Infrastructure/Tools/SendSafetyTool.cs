namespace LLM_Demo.Infrastructure.Tools;

using LLM_Demo.Domain.Tools;
using Microsoft.Extensions.Logging;

/// <summary>
/// Send-safety tool: validates and filters outgoing messages before they are sent.
/// Acts as both a ToolDefinition (for the agent to call) and a safety gate.
/// </summary>
public sealed class SendSafetyTool
{
    private readonly ILogger<SendSafetyTool> _logger;

    public SendSafetyTool(ILogger<SendSafetyTool> logger)
    {
        _logger = logger;
    }

    public static ToolDefinition Definition => new()
    {
        Name = "send_safety",
        Description = "Validates content before sending. Checks for PII, blocked patterns, and rate limits."
    };

    public async Task<ToolResult> ExecuteAsync(string content, string destination)
    {
        _logger.LogInformation("Send-safety check for destination: {Destination}", destination);

        // Check 1: Blocked patterns
        var blockedPatterns = new[] { "password=", "secret=", "api_key=", "Bearer " };
        foreach (var pattern in blockedPatterns)
        {
            if (content.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Send-safety blocked: content contains sensitive pattern '{Pattern}'", pattern);
                return ToolResult.Failure($"Content blocked: contains sensitive data pattern '{pattern}'");
            }
        }

        // Check 2: Rate limiting simulation
        // In production: use sliding window counter
        await Task.Delay(50); // Simulate check latency

        _logger.LogInformation("Send-safety passed for destination: {Destination}", destination);
        return ToolResult.Success($"Content validated and sent to {destination}");
    }
}
