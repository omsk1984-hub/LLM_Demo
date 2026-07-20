namespace LLM_Demo.Infrastructure.Middleware;

using LLM_Demo.Application.RAG;
using LLM_Demo.Domain.Agents;
using LLM_Demo.Domain.Middleware;
using LLM_Demo.Domain.Tools;
using LLM_Demo.Infrastructure.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// Dispatches tool calls to concrete tool implementations.
/// Parses JSON arguments and invokes the appropriate tool handler.
/// </summary>
public sealed class ToolDispatcher : IToolDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IToolRegistry _toolRegistry;
    private readonly ILogger<ToolDispatcher> _logger;

    public ToolDispatcher(
        IServiceProvider serviceProvider,
        IServiceScopeFactory scopeFactory,
        IToolRegistry toolRegistry,
        ILogger<ToolDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _scopeFactory = scopeFactory;
        _toolRegistry = toolRegistry;
        _logger = logger;
    }

    public async Task<ToolResult> ExecuteAsync(ToolMiddlewareContext context)
    {
        var toolName = context.ToolCall.Name;
        var toolDef = _toolRegistry.GetTool(toolName);

        if (toolDef is null)
        {
            _logger.LogWarning("Tool '{ToolName}' not found in registry", toolName);
            return ToolResult.Failure($"Tool '{toolName}' is not available.");
        }

        _logger.LogDebug("Dispatching tool '{ToolName}' with arguments: {Arguments}",
            toolName, context.ToolCall.Arguments);

        return toolName.ToLowerInvariant() switch
        {
            "calculator" => ExecuteCalculator(context.ToolCall.Arguments),
            "file_system" => await ExecuteFileSystemAsync(context.ToolCall.Arguments),
            "send_safety" => await ExecuteSendSafetyAsync(context.ToolCall.Arguments),
            "search_documents" => await ExecuteSearchDocumentsAsync(context.ToolCall.Arguments, context.Agent),
            _ => ToolResult.Failure($"Tool '{toolName}' has no handler registered.")
        };
    }

    private async Task<ToolResult> ExecuteSearchDocumentsAsync(string arguments, Agent? agent)
    {
        try
        {
            if (agent is null)
                return ToolResult.Failure("search_documents requires an agent context.");

            using var doc = System.Text.Json.JsonDocument.Parse(arguments);
            var query = doc.RootElement.GetProperty("query").GetString() ?? "";

            // VectorSearchService — Scoped, поэтому создаём scope
            using var scope = _scopeFactory.CreateScope();
            var vectorSearch = scope.ServiceProvider.GetRequiredService<VectorSearchService>();
            var ragTool = new RagTool(vectorSearch);
            return await ragTool.ExecuteAsync(arguments, agent.Id);
        }
        catch (Exception ex)
        {
            return ToolResult.Failure($"search_documents error: {ex.Message}");
        }
    }

    private ToolResult ExecuteCalculator(string arguments)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(arguments);
            var expression = doc.RootElement.GetProperty("expression").GetString() ?? "";
            var calculator = new CalculatorTool();
            return calculator.Execute(expression);
        }
        catch (Exception ex)
        {
            return ToolResult.Failure($"Calculator error: {ex.Message}");
        }
    }

    private async Task<ToolResult> ExecuteFileSystemAsync(string arguments)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(arguments);
            var action = doc.RootElement.GetProperty("action").GetString() ?? "";
            var filename = doc.RootElement.GetProperty("filename").GetString() ?? "";
            var content = doc.RootElement.TryGetProperty("content", out var contentEl)
                ? contentEl.GetString()
                : null;

            var fsTool = ActivatorUtilities.CreateInstance<FileSystemTool>(_serviceProvider);
            return await fsTool.ExecuteAsync(action, filename, content);
        }
        catch (Exception ex)
        {
            return ToolResult.Failure($"FileSystem error: {ex.Message}");
        }
    }

    private async Task<ToolResult> ExecuteSendSafetyAsync(string arguments)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(arguments);
            var content = doc.RootElement.GetProperty("content").GetString() ?? "";
            var destination = doc.RootElement.GetProperty("destination").GetString() ?? "";

            var safetyTool = ActivatorUtilities.CreateInstance<SendSafetyTool>(_serviceProvider);
            return await safetyTool.ExecuteAsync(content, destination);
        }
        catch (Exception ex)
        {
            return ToolResult.Failure($"SendSafety error: {ex.Message}");
        }
    }
}
