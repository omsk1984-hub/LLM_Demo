namespace LLM_Demo.Infrastructure.RAG;

using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LLM_Demo.Domain.RAG;

/// <summary>
/// Generates embeddings using any OpenAI-compatible embeddings API (/embeddings endpoint).
/// </summary>
internal sealed class OpenAIEmbeddingClient : IEmbeddingClient
{
    private readonly HttpClient _httpClient;
    private readonly string _endpoint;
    private readonly string _modelId;
    private readonly string? _apiKey;

    public OpenAIEmbeddingClient(HttpClient httpClient, EmbeddingOptions options)
    {
        _httpClient = httpClient;
        _endpoint = options.Endpoint.TrimEnd('/');
        _modelId = options.ModelId;
        _apiKey = string.IsNullOrWhiteSpace(options.ApiKey) ? null : options.ApiKey;
        Dimensions = options.Dimensions;
    }

    public int Dimensions { get; }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct = default)
    {
        var results = await GenerateEmbeddingsAsync([text], ct);
        return results[0];
    }

    public async Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(
        IEnumerable<string> texts,
        CancellationToken ct = default)
    {
        var textsList = texts.ToList();
        if (textsList.Count == 0)
            return Array.Empty<float[]>();

        var requestBody = new
        {
            model = _modelId,
            input = textsList.Count == 1 ? textsList[0] : (object)textsList
        };

        var json = JsonSerializer.Serialize(requestBody);
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_endpoint}/embeddings")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        if (!string.IsNullOrEmpty(_apiKey))
        {
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {_apiKey}");
        }

        var response = await _httpClient.SendAsync(request, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Embedding API returned {(int)response.StatusCode} ({response.ReasonPhrase}) " +
                $"for endpoint '{_endpoint}/embeddings'. [Model: {_modelId}] Response: {responseBody}");
        }

        var result = JsonSerializer.Deserialize<EmbeddingApiResponse>(responseBody);
        if (result?.data is null || result.data.Count == 0)
        {
            throw new InvalidOperationException(
                $"Embedding API returned empty response. Response: {responseBody}");
        }

        // Sort by index to preserve order
        return result.data
            .OrderBy(d => d.index)
            .Select(d => d.embedding)
            .ToList();
    }

    // ── JSON response models ────────────────────────────────────

    private sealed class EmbeddingApiResponse
    {
        [JsonPropertyName("data")]
        public List<EmbeddingData>? data { get; set; }
    }

    private sealed class EmbeddingData
    {
        [JsonPropertyName("index")]
        public int index { get; set; }

        [JsonPropertyName("embedding")]
        public float[] embedding { get; set; } = [];
    }
}
