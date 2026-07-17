namespace LLM_Demo.Application.AgentLoop;

using LLM_Demo.Domain.Agents;
using LLM_Demo.Domain.Conversations;
using LLM_Demo.Domain.Messages;
using LLM_Demo.Domain.Tools;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

public sealed class MAFAgentLoopOptions
{
    public int MaxIterations { get; set; } = 10;
    public bool StopOnToolError { get; set; } = false;
}

public sealed class MAFAgentLoop : IAgentLoop
{
    private readonly IChatClient _chatClient;
    private readonly Func<ToolCall, Agent?, CancellationToken, Task<ToolResult>> _toolExecutor;
    private readonly MAFAgentLoopOptions _options;
    private readonly ILogger<MAFAgentLoop> _logger;

    public MAFAgentLoop(
        IChatClient chatClient,
        Func<ToolCall, Agent?, CancellationToken, Task<ToolResult>> toolExecutor,
        ILogger<MAFAgentLoop> logger,
        MAFAgentLoopOptions? options = null)
    {
        _chatClient = chatClient;
        _toolExecutor = toolExecutor;
        _logger = logger;
        _options = options ?? new MAFAgentLoopOptions();
    }

    public async Task<AgentLoopResult> ExecuteAsync(
        Conversation conversation,
        Agent agent,
        CancellationToken ct = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, agent.SystemPrompt)
        };

        var iterations = 0;

        while (iterations < _options.MaxIterations)
        {
            ct.ThrowIfCancellationRequested();
            iterations++;

            _logger.LogDebug("Agent loop iteration {Iteration}/{MaxIterations}", iterations, _options.MaxIterations);

            var response = await _chatClient.CompleteAsync(messages, cancellationToken: ct);
            var reply = response.Message;

            messages.Add(reply);

            // Check if the response contains tool calls
            var toolCalls = ExtractToolCalls(reply);

            if (toolCalls.Count == 0)
            {
                // Final response from LLM
                stopwatch.Stop();
                return AgentLoopResult.Success(
                    messages.Select(m => new Message
                    {
                        Role = MapRole(m.Role),
                        Content = m.Text ?? "",
                        ConversationId = conversation.Id
                    }).ToList(),
                    iterations,
                    stopwatch.Elapsed);
            }

            // Process each tool call
            foreach (var toolCall in toolCalls)
            {
                var toolResult = await _toolExecutor(toolCall, agent, ct);
                messages.Add(new ChatMessage(ChatRole.Tool, toolResult.Result));

                if (!toolResult.IsSuccess && _options.StopOnToolError)
                {
                    stopwatch.Stop();
                    return AgentLoopResult.Failure(toolResult.Error ?? "Tool execution failed");
                }
            }
        }

        stopwatch.Stop();
        _logger.LogWarning("Agent loop reached max iterations ({MaxIterations})", _options.MaxIterations);

        return AgentLoopResult.Success(
            messages.Select(m => new Message
            {
                Role = MapRole(m.Role),
                Content = m.Text ?? "",
                ConversationId = conversation.Id
            }).ToList(),
            iterations,
            stopwatch.Elapsed);
    }

    public async IAsyncEnumerable<StreamingChunk> ExecuteStreamingAsync(
        Conversation conversation,
        Agent agent,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, agent.SystemPrompt)
        };

        await foreach (var update in _chatClient.CompleteStreamingAsync(messages, cancellationToken: ct))
        {
            ct.ThrowIfCancellationRequested();

            yield return new StreamingChunk
            {
                Content = update.Text ?? ""
            };
        }

        yield return new StreamingChunk { IsFinal = true };
    }

    private static List<ToolCall> ExtractToolCalls(ChatMessage message)
    {
        // In MEAI preview, tool calls are embedded in Contents
        var toolCalls = new List<ToolCall>();

        if (message.Contents is { Count: > 0 })
        {
            foreach (var content in message.Contents)
            {
                if (content is FunctionCallContent fc)
                {
                    toolCalls.Add(new ToolCall
                    {
                        Id = fc.CallId ?? Guid.NewGuid().ToString(),
                        Name = fc.Name,
                        Arguments = System.Text.Json.JsonSerializer.Serialize(fc.Arguments)
                    });
                }
            }
        }

        return toolCalls;
    }

    private static MessageRole MapRole(ChatRole role)
    {
        if (role == ChatRole.System) return MessageRole.System;
        if (role == ChatRole.Assistant) return MessageRole.Assistant;
        if (role == ChatRole.Tool) return MessageRole.Tool;
        return MessageRole.User;
    }
}
