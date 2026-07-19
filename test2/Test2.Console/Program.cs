using System.Text;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// --- Configuration ---
var appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
if (!File.Exists(appSettingsPath))
    appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");

var config = JsonSerializer.Deserialize<OpenAiConfig>(
    File.ReadAllText(appSettingsPath),
    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
) ?? throw new InvalidOperationException("Failed to load appsettings.json");

var openAiSection = config.OpenAI;

// Prompt for API key if not set
var apiKey = openAiSection.ApiKey;
if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.Write("Enter your OpenAI API key: ");
    apiKey = Console.ReadLine() ?? string.Empty;
    Console.WriteLine();
}

// --- Host with DI ---
var builder = Host.CreateApplicationBuilder(args);

// Create HttpClient that ignores SSL errors (для routerai.ru и других кастомных endpoint'ов)
var httpClient = new HttpClient(new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = (_, _, _, _) => true
})
{
    Timeout = TimeSpan.FromSeconds(120)
};

// Используем прямой HTTP-клиент (IChatClient) без OpenAI SDK
// Это решает проблему SSL/TLS, т.к. мы полностью контролируем HttpClient
var endpoint = openAiSection.Endpoint.TrimEnd('/');

builder.Services.AddChatClient(_ => new DirectHttpChatClient(httpClient, endpoint, openAiSection.ModelId, apiKey));

var host = builder.Build();

// --- Chat Loop ---
var chatClient = host.Services.GetRequiredService<IChatClient>();

Console.WriteLine($"\n=== OpenAI Chat Console ===");
Console.WriteLine($"Model: {openAiSection.ModelId}");
Console.WriteLine($"Endpoint: {openAiSection.Endpoint}");
Console.WriteLine("Type 'exit' to quit, 'clear' to clear history.\n");

var messages = new List<ChatMessage>
{
    new(ChatRole.System, "You are a helpful assistant.")
};

// Если передан аргумент командной строки — используем его как вопрос и выходим
if (args.Length > 0)
{
    var question = string.Join(" ", args);
    Console.WriteLine($"You: {question}");

    messages.Add(new ChatMessage(ChatRole.User, question));
    Console.Write("Assistant: ");

    await foreach (var update in chatClient.CompleteStreamingAsync(messages))
    {
        if (!string.IsNullOrEmpty(update.Text))
        {
            Console.Write(update.Text);
        }
    }
    Console.WriteLine("\n");
    return;
}

while (true)
{
    Console.Write("You: ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input)) continue;
    if (input.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;
    if (input.Equals("clear", StringComparison.OrdinalIgnoreCase))
    {
        messages.Clear();
        messages.Add(new ChatMessage(ChatRole.System, "You are a helpful assistant."));
        Console.WriteLine("Chat history cleared.\n");
        continue;
    }

    messages.Add(new ChatMessage(ChatRole.User, input));

    Console.Write("Assistant: ");
    var fullResponse = new StringBuilder();

    // Streaming response
    await foreach (var update in chatClient.CompleteStreamingAsync(messages))
    {
        if (!string.IsNullOrEmpty(update.Text))
        {
            Console.Write(update.Text);
            fullResponse.Append(update.Text);
        }
    }
    Console.WriteLine("\n");

    messages.Add(new ChatMessage(ChatRole.Assistant, fullResponse.ToString()));
}

internal sealed class OpenAiConfig
{
    public OpenAiSettings OpenAI { get; set; } = new();
}

internal sealed class OpenAiSettings
{
    public string Endpoint { get; set; } = "https://routerai.ru/api/v1";
    public string ModelId { get; set; } = "qwen/qwen3.5-9b";
    public string ApiKey { get; set; } = "";
}

/// <summary>
/// Прямая HTTP-реализация IChatClient для OpenAI-совместимых API.
/// Не использует OpenAI SDK — только чистый HttpClient с полным контролем SSL.
/// </summary>
internal sealed class DirectHttpChatClient : IChatClient
{
    private readonly HttpClient _httpClient;
    private readonly string _endpoint;
    private readonly string _modelId;
    private readonly string _apiKey;
    private bool _disposed;

    public DirectHttpChatClient(HttpClient httpClient, string endpoint, string modelId, string? apiKey)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _endpoint = endpoint.TrimEnd('/');
        _modelId = modelId;
        _apiKey = apiKey ?? string.Empty;
    }

    public ChatClientMetadata Metadata { get; } = new("direct-http-chat-client");

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

        var json = JsonSerializer.Serialize(requestBody);
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_endpoint}/chat/completions")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {_apiKey}");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"API error {(int)response.StatusCode}: {responseBody}");
        }

        var result = JsonSerializer.Deserialize<ChatResponse>(responseBody);
        var messageText = result?.choices?.FirstOrDefault()?.message?.content ?? "";
        return new ChatCompletion(new ChatMessage(ChatRole.Assistant, messageText));
    }

    public async IAsyncEnumerable<StreamingChatCompletionUpdate> CompleteStreamingAsync(
        IList<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
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

        var json = JsonSerializer.Serialize(requestBody);
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_endpoint}/chat/completions")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {_apiKey}");

        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"API error {(int)response.StatusCode}: {errorBody}");
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrEmpty(line)) continue;
            if (!line.StartsWith("data: ")) continue;

            var data = line[6..];
            if (data == "[DONE]") yield break;

            var chunk = JsonSerializer.Deserialize<StreamingResponse>(data);
            if (chunk?.choices?.FirstOrDefault()?.delta?.content is { } delta)
            {
                yield return new StreamingChatCompletionUpdate { Text = delta };
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

    // JSON models for OpenAI-compatible chat API
    private sealed class ChatResponse
    {
        public Choice[]? choices { get; set; }
    }

    private sealed class Choice
    {
        public Message? message { get; set; }
    }

    private sealed class Message
    {
        public string? content { get; set; }
    }

    private sealed class StreamingResponse
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
    }
}
