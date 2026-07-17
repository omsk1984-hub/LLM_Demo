namespace LLM_Demo.Infrastructure.Connectors;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

/// <summary>
/// Factory for creating IChatClient instances for various LLM providers.
/// NOTE: Microsoft.Extensions.AI.OpenAI preview package API varies.
/// Update this when the stable package is released.
/// For now, use Ollama or direct OpenAI HTTP client.
/// </summary>
public static class OpenAIConnectorFactory
{
    public static IChatClient CreateOpenAIClient(string apiKey, string modelId = "gpt-4")
    {
        // Placeholder — will be implemented when Microsoft.Extensions.AI.OpenAI stabilizes.
        // The preview version requires: new OpenAI.OpenAIClient(apiKey).AsChatClient(modelId)
        throw new NotImplementedException(
            "OpenAI connector requires Microsoft.Extensions.AI.OpenAI stable package. " +
            "Use OllamaConnector or implement custom IChatClient wrapper.");
    }

    public static IChatClient CreateOllamaClient(string endpoint = "http://localhost:11434", string modelId = "llama3")
    {
        // Ollama exposes OpenAI-compatible API
        var httpClient = new HttpClient { BaseAddress = new Uri(endpoint) };
        // Return a simple wrapper — for demo purposes this shows the pattern
        return new OllamaChatClient(httpClient, modelId);
    }
}

/// <summary>
/// Lightweight IChatClient implementation for Ollama API.
/// In production, replace with a full implementation or use Semantic Kernel connectors.
/// </summary>
internal sealed class OllamaChatClient : IChatClient
{
    private readonly HttpClient _httpClient;
    private readonly string _modelId;

    public OllamaChatClient(HttpClient httpClient, string modelId)
    {
        _httpClient = httpClient;
        _modelId = modelId;
    }

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Build Ollama-compatible request body
        var request = new
        {
            model = _modelId,
            messages = messages.Select(m => new
            {
                role = m.Role?.ToString()?.ToLowerInvariant() ?? "user",
                content = m.Text
            }),
            stream = false
        };

        var json = System.Text.Json.JsonSerializer.Serialize(request);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/api/chat", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = System.Text.Json.JsonSerializer.Deserialize<OllamaChatResponse>(responseJson);

        return new ChatResponse(new ChatMessage(ChatRole.Assistant, result?.message?.content ?? ""));
    }

    public IAsyncEnumerable<StreamingChatMessage> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Streaming not implemented in this demo connector.");
    }

    public void Dispose() { }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    private sealed class OllamaChatResponse
    {
        public OllamaMessage? message { get; set; }
    }

    private sealed class OllamaMessage
    {
        public string? content { get; set; }
    }
}
