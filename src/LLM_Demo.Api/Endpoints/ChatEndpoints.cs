namespace LLM_Demo.Api.Endpoints;

using System.Text.Json;
using LLM_Demo.Api.Extensions;
using LLM_Demo.Api.Models.Requests;
using LLM_Demo.Api.Models.Responses;
using LLM_Demo.Application.AgentLoop;
using LLM_Demo.Domain.Agents;
using LLM_Demo.Domain.Connectors;
using LLM_Demo.Domain.Messages;
using LLM_Demo.Domain.Tools;
using LLM_Demo.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.AI;

public sealed class ChatEndpoints
{
    private readonly AgentRepository _agentRepository;
    private readonly ConversationRepository _conversationRepository;
    private readonly IConnectorProvider _connectorProvider;
    private readonly ILogger<ChatEndpoints> _logger;
    private readonly ILogger<MAFAgentLoop> _loopLogger;

    public ChatEndpoints(
        AgentRepository agentRepository,
        ConversationRepository conversationRepository,
        IConnectorProvider connectorProvider,
        ILogger<ChatEndpoints> logger,
        ILogger<MAFAgentLoop> loopLogger)
    {
        _agentRepository = agentRepository;
        _conversationRepository = conversationRepository;
        _connectorProvider = connectorProvider;
        _logger = logger;
        _loopLogger = loopLogger;
    }

    public async Task<IResult> Chat(Guid agentId, ChatRequest request, HttpContext httpContext)
    {
        var agent = await _agentRepository.GetByIdAsync(agentId);
        if (agent is null)
            return Results.NotFound(new ErrorResponse("Agent not found"));

        var conversation = await _conversationRepository.GetByIdAsync(request.ConversationId);
        if (conversation is null)
            return Results.NotFound(new ErrorResponse("Conversation not found"));

        // Сохраняем новое сообщение пользователя
        var userMessage = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Role = MessageRole.User,
            Content = request.Message
        };
        await _conversationRepository.AddMessageAsync(userMessage);

        // Просто сохраняем сообщение и возвращаем 202 Accepted.
        // Сам LLM-запрос выполняется асинхронно через SSE-стриминг (ChatStream).
        _logger.LogInformation("User message saved for conversation {ConversationId}. LLM response will be streamed via SSE.", conversation.Id);

        return Results.Accepted($"/api/chat/{agentId}/stream?conversationId={conversation.Id}");
    }

    public async Task ChatStream(
        Guid agentId,
        Guid conversationId,
        HttpContext httpContext)
    {
        httpContext.Response.ContentType = "text/event-stream";
        httpContext.Response.Headers.CacheControl = "no-cache";
        httpContext.Response.Headers.Connection = "keep-alive";

        var userId = httpContext.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            await WriteSseAsync(httpContext, "error", "User not authenticated");
            return;
        }

        var agent = await _agentRepository.GetByIdAsync(agentId);
        if (agent is null)
        {
            await WriteSseAsync(httpContext, "error", "Agent not found");
            return;
        }

        // Проверяем ownership агента
        if (agent.OwnerId != userId)
        {
            await WriteSseAsync(httpContext, "error", "Agent not found");
            return;
        }

        var conversation = await _conversationRepository.GetByIdAsync(conversationId);
        if (conversation is null)
        {
            await WriteSseAsync(httpContext, "error", "Conversation not found");
            return;
        }

        // Проверяем ownership беседы
        if (conversation.OwnerId != userId)
        {
            await WriteSseAsync(httpContext, "error", "Conversation not found");
            return;
        }

        // Загружаем исторические сообщения (новое сообщение уже сохранено через REST)
        var historyMessages = (await _conversationRepository.GetMessagesAsync(conversation.Id)).ToList();

        var loop = new MAFAgentLoop(
            _connectorProvider,
            (toolCall, agent, ct) => Task.FromResult(ToolResult.Success($"Executed {toolCall.Name}")),
            _loopLogger);

        try
        {
            var ct = httpContext.RequestAborted;
            await foreach (var chunk in loop.ExecuteStreamingAsync(conversation, agent, historyMessages, ct: ct))
            {
                var json = JsonSerializer.Serialize(chunk, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                await WriteSseAsync(httpContext, "chunk", json);

                if (chunk.IsFinal)
                {
                    await WriteSseAsync(httpContext, "complete", "{}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Streaming cancelled by user for conversation {ConversationId}", conversationId);
            await WriteSseAsync(httpContext, "cancelled", "{}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Streaming error for conversation {ConversationId}", conversationId);
            try
            {
                await WriteSseAsync(httpContext, "error", ex.Message);
            }
            catch
            {
                // Если ответ уже был отправлен, ничего не делаем
            }
        }
    }

    private static async Task WriteSseAsync(HttpContext context, string eventType, string data)
    {
        await context.Response.WriteAsync($"event: {eventType}\n");
        await context.Response.WriteAsync($"data: {data}\n\n");
        await context.Response.Body.FlushAsync();
    }
}

/// <summary>
/// Simple echo chat client for demo/testing when no real IChatClient is configured.
/// </summary>
internal sealed class EchoChatClient : IChatClient
{
    public ChatClientMetadata Metadata { get; } = new("echo-chat-client");

    public Task<ChatCompletion> CompleteAsync(
        IList<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var lastUserMessage = chatMessages.LastOrDefault(m => m.Role == ChatRole.User);
        var reply = $"Echo: {lastUserMessage?.Text ?? "Hello! I'm a demo agent."}";

        return Task.FromResult(new ChatCompletion(new ChatMessage(ChatRole.Assistant, reply)));
    }

    public async IAsyncEnumerable<StreamingChatCompletionUpdate> CompleteStreamingAsync(
        IList<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var lastUserMessage = chatMessages.LastOrDefault(m => m.Role == ChatRole.User);
        var reply = $"Echo: {lastUserMessage?.Text ?? "Hello!"}";

        foreach (var ch in reply)
        {
            yield return new StreamingChatCompletionUpdate { Text = ch.ToString() };
            await Task.Delay(30, cancellationToken);
        }
    }

    public void Dispose() { }

    public TService? GetService<TService>(object? key = null) where TService : class => null;
}
