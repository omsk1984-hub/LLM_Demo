namespace LLM_Demo.Infrastructure.Connectors;

using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

/// <summary>
/// Echo-клиент для демо/тестирования, когда не настроен реальный LLM-провайдер.
/// Просто возвращает введённый пользователем текст с префиксом "Echo: ".
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
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
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
