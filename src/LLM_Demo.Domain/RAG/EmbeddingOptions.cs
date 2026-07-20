namespace LLM_Demo.Domain.RAG;

/// <summary>
/// Configuration options for the embedding client.
/// </summary>
public sealed class EmbeddingOptions
{
    public const string SectionName = "Embedding";

    public string Endpoint { get; set; } = string.Empty;
    public string ModelId { get; set; } = "text-embedding-ada-002";
    public string? ApiKey { get; set; }
    public int Dimensions { get; set; } = 1536;
}
