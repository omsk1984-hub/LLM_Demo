namespace LLM_Demo.Infrastructure.Connectors;

using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

/// <summary>
/// Factory for creating IChatClient instances for various LLM providers.
/// </summary>
public static class OpenAIConnectorFactory
{
    /// <summary>
    /// Creates an IChatClient configured for a generic OpenAI-compatible API via HTTP.
    /// Supports any OpenAI-compatible endpoint (OpenAI, Azure, Ollama, etc.).
    /// </summary>
    public static IChatClient CreateHttpChatClient(
        HttpClient httpClient,
        string endpoint,
        string modelId,
        string? apiKey = null)
    {
        return new OllamaStyleChatClient(httpClient, endpoint, modelId, apiKey);
    }
}

/// <summary>
/// Lightweight IChatClient implementation for OpenAI-compatible chat APIs
/// using a simple HTTP client. Supports Ollama, OpenAI, Azure OpenAI, etc.
/// </summary>
internal sealed class OllamaStyleChatClient : IChatClient
{
    private readonly HttpClient _httpClient;
    private readonly string _endpoint;
    private readonly string _modelId;
    private readonly string? _apiKey;
    private bool _disposed;

    public OllamaStyleChatClient(HttpClient httpClient, string endpoint, string modelId, string? apiKey = null)
    {
        _httpClient = httpClient;
        _endpoint = endpoint.TrimEnd('/');
        _modelId = modelId;
        _apiKey = apiKey;
    }

    public ChatClientMetadata Metadata { get; } = new("ollama-style-chat-client");

    public async Task<ChatCompletion> CompleteAsync(
        IList<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var requestBody = new
        {
            model = _modelId,
            messages = chatMessages.Select(m => new
            {
                role = ConvertRole(m.Role),
                content = m.Text
            }),
            stream = false
        };

        var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        if (!string.IsNullOrEmpty(_apiKey))
        {
            content.Headers.TryAddWithoutValidation("Authorization", $"Bearer {_apiKey}");
        }

        var response = await _httpClient.PostAsync($"{_endpoint}/chat/completions", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = System.Text.Json.JsonSerializer.Deserialize<ChatCompletionResponse>(responseJson);

        var message = result?.choices?.FirstOrDefault()?.message;
        return new ChatCompletion(new ChatMessage(ChatRole.Assistant, message?.content ?? ""));
    }

    public async IAsyncEnumerable<StreamingChatCompletionUpdate> CompleteStreamingAsync(
        IList<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var requestBody = new
        {
            model = _modelId,
            messages = chatMessages.Select(m => new
            {
                role = ConvertRole(m.Role),
                content = m.Text
            }),
            stream = true
        };

        var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        if (!string.IsNullOrEmpty(_apiKey))
        {
            content.Headers.TryAddWithoutValidation("Authorization", $"Bearer {_apiKey}");
        }

        var response = await _httpClient.PostAsync($"{_endpoint}/chat/completions", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrEmpty(line) || !line.StartsWith("data: ")) continue;

            var data = line[6..];
            if (data == "[DONE]") yield break;

            var chunk = System.Text.Json.JsonSerializer.Deserialize<StreamingChunkResponse>(data);
            if (chunk?.choices?.FirstOrDefault()?.delta?.content is { } delta)
            {
                yield return new StreamingChatCompletionUpdate
                {
                    Text = delta
                };
            }
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient.Dispose();
            _disposed = true;
        }
    }

    public TService? GetService<TService>(object? key = null) where TService : class => null;

    private static string ConvertRole(ChatRole role)
    {
        if (role == ChatRole.User) return "user";
        if (role == ChatRole.Assistant) return "assistant";
        if (role == ChatRole.System) return "system";
        if (role == ChatRole.Tool) return "tool";
        return "user";
    }

    // JSON response models
    private sealed class ChatCompletionResponse
    {
        public Choice[]? choices { get; set; }
    }

    private sealed class Choice
    {
        public ResponseMessage? message { get; set; }
    }

    private sealed class ResponseMessage
    {
        public string? content { get; set; }
    }

    private sealed class StreamingChunkResponse
    {
        public StreamingChoice[]? choices { get; set; }
    }

    private sealed class StreamingChoice
    {
        public StreamingDelta? delta { get; set; }
    }

    private sealed class StreamingDelta
    {
        public string? content { get; set; }
    }
}
