namespace LLM_Demo.Application.AgentLoop;

using LLM_Demo.Domain.Agents;
using LLM_Demo.Domain.Connectors;
using LLM_Demo.Domain.Conversations;
using LLM_Demo.Domain.Messages;
using LLM_Demo.Domain.Tools;
using System.Text;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

public sealed class MAFAgentLoopOptions
{
    public int MaxIterations { get; set; } = 10;
    public bool StopOnToolError { get; set; } = false;
}

public sealed class MAFAgentLoop : IAgentLoop
{
    private readonly IConnectorProvider _connectorProvider;
    private readonly Func<ToolCall, Agent?, CancellationToken, Task<ToolResult>> _toolExecutor;
    private readonly MAFAgentLoopOptions _options;
    private readonly ILogger<MAFAgentLoop> _logger;

    public MAFAgentLoop(
        IConnectorProvider connectorProvider,
        Func<ToolCall, Agent?, CancellationToken, Task<ToolResult>> toolExecutor,
        ILogger<MAFAgentLoop> logger,
        MAFAgentLoopOptions? options = null)
    {
        _connectorProvider = connectorProvider;
        _toolExecutor = toolExecutor;
        _logger = logger;
        _options = options ?? new MAFAgentLoopOptions();
    }

    /// <summary>
    /// Возвращает IChatClient для указанного агента на основе его ConnectorName.
    /// Если у агента указана ModelId, пробрасывает её через ChatOptions.
    /// </summary>
    private IChatClient GetChatClient(Agent agent)
    {
        var client = _connectorProvider.GetClient(agent.ConnectorName);
        return client;
    }

    private static ChatOptions? BuildChatOptions(Agent agent)
    {
        if (string.IsNullOrWhiteSpace(agent.ModelId))
            return null;

        return new ChatOptions
        {
            ModelId = agent.ModelId
        };
    }

    public async Task<AgentLoopResult> ExecuteAsync(
        Conversation conversation,
        Agent agent,
        IReadOnlyList<Message>? historyMessages = null,
        string? newUserMessage = null,
        CancellationToken ct = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, agent.SystemPrompt)
        };

        // Добавляем исторические сообщения из conversation
        if (historyMessages is { Count: > 0 })
        {
            foreach (var msg in historyMessages)
            {
                messages.Add(new ChatMessage(MapRoleToChatRole(msg.Role), msg.Content));
            }
        }

        // Добавляем новое сообщение пользователя, если есть
        if (!string.IsNullOrWhiteSpace(newUserMessage))
        {
            messages.Add(new ChatMessage(ChatRole.User, newUserMessage));
        }

        var chatClient = GetChatClient(agent);
        var chatOptions = BuildChatOptions(agent);
        
        var iterations = 0;

        while (iterations < _options.MaxIterations)
        {
            ct.ThrowIfCancellationRequested();
            iterations++;

            _logger.LogDebug("Agent loop iteration {Iteration}/{MaxIterations} for agent '{Agent}' via connector '{Connector}'",
                iterations, _options.MaxIterations, agent.Name, agent.ConnectorName);

            var response = await chatClient.CompleteAsync(messages, chatOptions, cancellationToken: ct);
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
        IReadOnlyList<Message>? historyMessages = null,
        string? newUserMessage = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, agent.SystemPrompt)
        };

        // Добавляем исторические сообщения из conversation
        if (historyMessages is { Count: > 0 })
        {
            foreach (var msg in historyMessages)
            {
                messages.Add(new ChatMessage(MapRoleToChatRole(msg.Role), msg.Content));
            }
        }

        // Добавляем новое сообщение пользователя, если есть
        if (!string.IsNullOrWhiteSpace(newUserMessage))
        {
            messages.Add(new ChatMessage(ChatRole.User, newUserMessage));
        }

        var chatClient = GetChatClient(agent);
        var chatOptions = BuildChatOptions(agent);
        var iterations = 0;

        while (iterations < _options.MaxIterations)
        {
            ct.ThrowIfCancellationRequested();
            iterations++;

            _logger.LogDebug("Agent loop iteration {Iteration}/{MaxIterations} for agent '{Agent}' via connector '{Connector}'",
                iterations, _options.MaxIterations, agent.Name, agent.ConnectorName);

            var fullText = new StringBuilder();
            var toolCallAccumulators = new Dictionary<string, (string Name, StringBuilder Args)>();

            // Streaming LLM ответа
            await foreach (var update in chatClient.CompleteStreamingAsync(messages, chatOptions, cancellationToken: ct))
            {
                ct.ThrowIfCancellationRequested();

                // Аккумулируем текст и сразу отдаём чанки наружу
                if (update.Text is { Length: > 0 })
                {
                    fullText.Append(update.Text);
                    yield return new StreamingChunk { Content = update.Text };
                }

                // Аккумулируем tool call updates (FunctionCallContent с _rawArguments в AdditionalProperties)
                if (update.Contents is { Count: > 0 })
                {
                    foreach (var content in update.Contents)
                    {
                        if (content is FunctionCallContent fc)
                        {
                            var callId = fc.CallId ?? Guid.NewGuid().ToString();

                            // Name приходит только в первом чанке для данного tool call
                            if (!string.IsNullOrEmpty(fc.Name))
                            {
                                toolCallAccumulators[callId] = (fc.Name, new StringBuilder());
                            }

                            // Фрагменты аргументов приходят через AdditionalProperties["_rawArguments"]
                            if (fc.AdditionalProperties?.TryGetValue("_rawArguments", out var rawArg) == true &&
                                rawArg is string argFragment &&
                                !string.IsNullOrEmpty(argFragment))
                            {
                                if (!toolCallAccumulators.TryGetValue(callId, out var acc))
                                {
                                    acc = (fc.Name ?? "unknown", new StringBuilder());
                                    toolCallAccumulators[callId] = acc;
                                }
                                acc.Args.Append(argFragment);
                            }
                        }
                    }
                }
            }

            // Если есть tool calls — выполняем их и идём на следующую итерацию
            if (toolCallAccumulators.Count > 0)
            {
                // Создаём assistant сообщение с FunctionCallContent для сохранения контекста
                // ВАЖНО: передаём накопленные аргументы, чтобы при повторной отправке запроса
                // LLM API получила полные tool_calls
                var assistantContents = new List<AIContent>();
                foreach (var kv in toolCallAccumulators)
                {
                    var accumulatedArgs = kv.Value.Args.ToString();
                    IDictionary<string, object?>? argsDict = null;
                    if (!string.IsNullOrEmpty(accumulatedArgs))
                    {
                        try
                        {
                            argsDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object?>>(accumulatedArgs);
                        }
                        catch
                        {
                            // Если не JSON-объект, передаём через _rawArguments
                        }
                    }

                    var fc = new FunctionCallContent(kv.Key, kv.Value.Name, arguments: argsDict);
                    // Если не удалось распарсить как JSON, передаём сырую строку
                    if (argsDict is null && !string.IsNullOrEmpty(accumulatedArgs))
                    {
                        fc.AdditionalProperties!["_rawArguments"] = accumulatedArgs;
                    }
                    assistantContents.Add(fc);
                }

                messages.Add(new ChatMessage(ChatRole.Assistant, fullText.ToString())
                {
                    Contents = assistantContents
                });

                // Выполняем каждый tool
                foreach (var kv in toolCallAccumulators)
                {
                    var (callId, (name, argsBuilder)) = kv;
                    var argumentsJson = argsBuilder.ToString();

                    var toolCall = new ToolCall
                    {
                        Id = callId,
                        Name = name,
                        Arguments = argumentsJson
                    };

                    _logger.LogDebug("Executing tool call: {ToolName} (id: {ToolCallId})", name, callId);

                    var toolResult = await _toolExecutor(toolCall, agent, ct);

                    yield return new StreamingChunk
                    {
                        Content = $"  {toolResult.Result}",
                        ToolCallId = callId
                    };

                    // ВАЖНО: передаём tool_call_id через Contents, чтобы BuildMessageDto
                    // в OpenAIConnector корректно сериализовал tool-сообщение
                    messages.Add(new ChatMessage(ChatRole.Tool, toolResult.Result)
                    {
                        Contents = [new FunctionCallContent(callId, name)]
                    });

                    if (!toolResult.IsSuccess && _options.StopOnToolError)
                    {
                        _logger.LogWarning("Tool execution failed: {ToolName} (id: {ToolCallId}) with error: {Error}",
                            name, callId, toolResult.Error);

                        yield return new StreamingChunk { Error = toolResult.Error ?? "Tool execution failed" };
                        yield return new StreamingChunk { IsFinal = true };
                        yield break;
                    }
                }

                continue; // следующая итерация агентского цикла
            }

            // Финальный ответ — без tool calls
            yield return new StreamingChunk { IsFinal = true };
            yield break;
        }

        // Достигнут лимит итераций
        _logger.LogWarning("Agent loop reached max iterations ({MaxIterations})", _options.MaxIterations);
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
                    string? arguments = null;

                    // Пытаемся получить arguments из fc.Arguments (IDictionary)
                    if (fc.Arguments is { Count: > 0 })
                    {
                        arguments = System.Text.Json.JsonSerializer.Serialize(fc.Arguments);
                    }

                    // Если есть _rawArguments в AdditionalProperties — используем их
                    if (arguments is null && fc.AdditionalProperties?.TryGetValue("_rawArguments", out var raw) == true && raw is string rawStr)
                    {
                        arguments = rawStr;
                    }

                    toolCalls.Add(new ToolCall
                    {
                        Id = fc.CallId ?? Guid.NewGuid().ToString(),
                        Name = fc.Name,
                        Arguments = arguments ?? ""
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

    private static ChatRole MapRoleToChatRole(MessageRole role)
    {
        if (role == MessageRole.System) return ChatRole.System;
        if (role == MessageRole.Assistant) return ChatRole.Assistant;
        if (role == MessageRole.Tool) return ChatRole.Tool;
        return ChatRole.User;
    }
}
