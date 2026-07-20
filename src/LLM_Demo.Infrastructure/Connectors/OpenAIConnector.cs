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
        return new OpenAIChatClient(httpClient, endpoint, modelId, apiKey);
    }
}

/// <summary>
/// Lightweight IChatClient implementation for OpenAI-compatible chat APIs
/// using a simple HTTP client. Supports Ollama, OpenAI, Azure OpenAI, etc.
/// </summary>
internal sealed class OpenAIChatClient : IChatClient
{
    private readonly HttpClient _httpClient;
    private readonly string _endpoint;
    private readonly string _modelId;
    private readonly string? _apiKey;
    private bool _disposed;

    public OpenAIChatClient(HttpClient httpClient, string endpoint, string modelId, string? apiKey = null)
    {
        _httpClient = httpClient;
        _endpoint = endpoint.TrimEnd('/');
        _modelId = modelId;
        _apiKey = apiKey;
    }

    public ChatClientMetadata Metadata { get; } = new("openai-chat-client");

    public async Task<ChatCompletion> CompleteAsync(
        IList<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeMessages(chatMessages);
        // ModelId из параметров метода имеет приоритет над моделью из конструктора
        var modelId = options?.ModelId ?? _modelId;

        var requestBody = new
        {
            model = modelId,
            messages = normalized.Select(m => new
            {
                role = ConvertRole(m.Role),
                content = m.Text
            }),
            stream = false
        };

        var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_endpoint}/chat/completions")
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };

        // Добавляем Authorization header на уровне запроса, как в test2/Test2.Console/Program.cs
        if (!string.IsNullOrEmpty(_apiKey))
        {
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {_apiKey}");
        }

        var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var apiKeyPreview = string.IsNullOrEmpty(_apiKey)
                ? "not configured"
                : $"configured ({_apiKey[..Math.Min(8, _apiKey.Length)]}...)";

            throw new HttpRequestException(
                $"LLM API returned {(int)response.StatusCode} ({response.ReasonPhrase}) " +
                $"for endpoint '{_endpoint}/chat/completions'. " +
                $"[Model: {modelId}, API Key: {apiKeyPreview}] " +
                $"Response body: {responseBody}");
        }

        var result = System.Text.Json.JsonSerializer.Deserialize<ChatCompletionResponse>(responseBody);
        var message = result?.choices?.FirstOrDefault()?.message;
        return new ChatCompletion(new ChatMessage(ChatRole.Assistant, message?.content ?? ""));
    }

    public async IAsyncEnumerable<StreamingChatCompletionUpdate> CompleteStreamingAsync(
        IList<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeMessages(chatMessages);
        // ModelId из параметров метода имеет приоритет над моделью из конструктора
        var modelId = options?.ModelId ?? _modelId;

        var requestBody = new
        {
            model = modelId,
            messages = normalized.Select(m => new
            {
                role = ConvertRole(m.Role),
                content = m.Text
            }),
            stream = true
        };

        var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_endpoint}/chat/completions")
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };

        // Добавляем Authorization header на уровне запроса, как в test2/Test2.Console/Program.cs
        if (!string.IsNullOrEmpty(_apiKey))
        {
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {_apiKey}");
        }

        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var apiKeyPreview = string.IsNullOrEmpty(_apiKey)
                ? "not configured"
                : $"configured ({_apiKey[..Math.Min(8, _apiKey.Length)]}...)";

            throw new HttpRequestException(
                $"LLM API returned {(int)response.StatusCode} ({response.ReasonPhrase}) " +
                $"for endpoint '{_endpoint}/chat/completions'. " +
                $"[Model: {modelId}, API Key: {apiKeyPreview}] " +
                $"Response body: {errorBody}");
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrEmpty(line) || !line.StartsWith("data: ")) continue;

            var data = line[6..];
            if (data == "[DONE]") yield break;

            var chunk = System.Text.Json.JsonSerializer.Deserialize<StreamingChunkResponse>(data);
            if (chunk?.choices?.FirstOrDefault()?.delta is { } delta)
            {
                var update = new StreamingChatCompletionUpdate();

                if (delta.content is { Length: > 0 })
                {
                    update.Text = delta.content;
                }

                if (delta.tool_calls is { Length: > 0 })
                {
                    var contents = new List<AIContent>();
                    foreach (var tc in delta.tool_calls)
                    {
                        if (tc.type == "function")
                        {
                            // FunctionCallContent принимает IDictionary<string,object?>? для аргументов.
                            // Сырую JSON-строку передадим через AdditionalProperties,
                            // чтобы MAFAgentLoop мог накопить фрагменты.
                            var fc = new FunctionCallContent(
                                tc.id ?? Guid.NewGuid().ToString(),
                                tc.function?.name,
                                arguments: null);

                            if (tc.function?.arguments != null)
                            {
                                fc.AdditionalProperties["_rawArguments"] = tc.function.arguments;
                            }

                            contents.Add(fc);
                        }
                    }
                    if (contents.Count > 0)
                    {
                        update.Contents = contents;
                    }
                }

                if (update.Text != null || (update.Contents?.Count > 0))
                {
                    yield return update;
                }
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

    /// <summary>
    /// Нормализует список сообщений для LLM API.
    /// 1. System-сообщение всегда должно быть первым (если есть).
    /// 2. Consecutive сообщения с одинаковой ролью (например, два User подряд)
    ///    объединяются в одно, т.к. многие провайдеры (SiliconFlow, Together)
    ///    требуют строгого чередования user/assistant.
    /// 3. Удаляет tool-сообщения, если они не следуют за assistant (некоторые API это не любят).
    /// </summary>
    private static List<ChatMessage> NormalizeMessages(IList<ChatMessage> messages)
    {
        if (messages.Count <= 1)
            return [.. messages];

        var result = new List<ChatMessage>(messages.Count);

        // Сначала отделяем system message (если есть)
        ChatMessage? system = null;
        foreach (var msg in messages)
        {
            if (msg.Role == ChatRole.System)
            {
                system = msg;
                break;
            }
        }

        // System message всегда должен быть первым
        if (system != null)
            result.Add(system);

        // Обрабатываем остальные сообщения, сливая consecutive same-role
        ChatRole? lastRole = null;
        foreach (var msg in messages)
        {
            // System уже обработан в начале
            if (msg.Role == ChatRole.System)
                continue;

            // Если роль совпадает с предыдущей — объединяем содержимое
            if (msg.Role == lastRole && result.Count > 0)
            {
                var last = result[^1];
                // Сливаем содержимое
                var combinedText = (last.Text ?? "") + "\n" + (msg.Text ?? "");
                result[^1] = new ChatMessage(msg.Role, combinedText);
                // Копируем Contents из последнего сообщения, если они есть
                if (msg.Contents is { Count: > 0 })
                {
                    var combinedContents = new List<AIContent>();
                    if (last.Contents is { Count: > 0 })
                        combinedContents.AddRange(last.Contents);
                    combinedContents.AddRange(msg.Contents);
                    result[^1].Contents = combinedContents;
                }
            }
            else
            {
                result.Add(msg);
                lastRole = msg.Role;
            }
        }

        return result;
    }

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
        public Delta? delta { get; set; }
    }

    private sealed class Delta
    {
        public string? content { get; set; }
        public ToolCallDelta[]? tool_calls { get; set; }
    }

    private sealed class ToolCallDelta
    {
        public string? id { get; set; }
        public string? type { get; set; }
        public FunctionDelta? function { get; set; }
    }

    private sealed class FunctionDelta
    {
        public string? name { get; set; }
        public string? arguments { get; set; }
    }
}
