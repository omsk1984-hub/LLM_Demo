namespace LLM_Demo.Api.Endpoints;

using System.Text.Json;
using LLM_Demo.Api.Models.Requests;
using LLM_Demo.Api.Models.Responses;
using LLM_Demo.Application.AgentLoop;
using LLM_Demo.Domain.Agents;
using LLM_Demo.Domain.Messages;
using LLM_Demo.Domain.Tools;
using LLM_Demo.Infrastructure.Persistence.Repositories;
using LLM_Demo.Infrastructure.Tools;
using Microsoft.Extensions.AI;

public sealed class ChatEndpoints
{
    private readonly AgentRepository _agentRepository;
    private readonly ConversationRepository _conversationRepository;
    private readonly IToolRegistry _toolRegistry;
    private readonly ILogger<ChatEndpoints> _logger;

    public ChatEndpoints(
        AgentRepository agentRepository,
        ConversationRepository conversationRepository,
        IToolRegistry toolRegistry,
        ILogger<ChatEndpoints> logger)
    {
        _agentRepository = agentRepository;
        _conversationRepository = conversationRepository;
        _toolRegistry = toolRegistry;
        _logger = logger;
    }

    public async Task<IResult> Chat(Guid agentId, ChatRequest request, HttpContext httpContext)
    {
        var agent = await _agentRepository.GetByIdAsync(agentId);
        if (agent is null)
            return Results.NotFound(new ErrorResponse("Agent not found"));

        var conversation = await _conversationRepository.GetByIdAsync(request.ConversationId);
        if (conversation is null)
            return Results.NotFound(new ErrorResponse("Conversation not found"));

        // Save user message
        await _conversationRepository.AddMessageAsync(new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Role = MessageRole.User,
            Content = request.Message
        });

        // For demo: use a simple echo chat client if no real IChatClient is configured
        var chatClient = httpContext.RequestServices.GetService<IChatClient>()
            ?? new EchoChatClient();

        var loopLogger = httpContext.RequestServices.GetRequiredService<ILogger<MAFAgentLoop>>();

        var loop = new MAFAgentLoop(
            chatClient,
            async (toolCall, agent, ct) =>
            {
                _logger.LogInformation("Tool called: {ToolName}", toolCall.Name);
                return ToolResult.Success($"Executed {toolCall.Name}");
            },
            loopLogger);

        var result = await loop.ExecuteAsync(conversation, agent);

        // Save all messages
        foreach (var msg in result.Messages)
        {
            msg.ConversationId = conversation.Id;
            await _conversationRepository.AddMessageAsync(msg);
        }

        return Results.Ok(new ChatResponse(
            result.Messages,
            result.Iterations,
            result.Duration));
    }

    public async Task ChatStream(
        Guid agentId,
        Guid conversationId,
        HttpContext httpContext)
    {
        httpContext.Response.ContentType = "text/event-stream";
        httpContext.Response.Headers.CacheControl = "no-cache";
        httpContext.Response.Headers.Connection = "keep-alive";

        var agent = await _agentRepository.GetByIdAsync(agentId);
        if (agent is null)
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

        var chatClient = httpContext.RequestServices.GetService<IChatClient>()
            ?? new EchoChatClient();

        var loopLogger = httpContext.RequestServices.GetRequiredService<ILogger<MAFAgentLoop>>();

        var loop = new MAFAgentLoop(
            chatClient,
            (toolCall, agent, ct) => Task.FromResult(ToolResult.Success($"Executed {toolCall.Name}")),
            loopLogger);

        try
        {
            await foreach (var chunk in loop.ExecuteStreamingAsync(conversation, agent))
            {
                var json = JsonSerializer.Serialize(chunk);
                await WriteSseAsync(httpContext, "chunk", json);

                if (chunk.IsFinal)
                {
                    await WriteSseAsync(httpContext, "complete", "{}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Streaming error");
            await WriteSseAsync(httpContext, "error", ex.Message);
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
