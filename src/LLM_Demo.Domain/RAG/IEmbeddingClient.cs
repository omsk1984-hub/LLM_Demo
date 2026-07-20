namespace LLM_Demo.Domain.RAG;

/// <summary>
/// Generates vector embeddings for text using an embedding model.
/// </summary>
public interface IEmbeddingClient
{
    /// <summary>
    /// Generates an embedding vector for the given text.
    /// </summary>
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct = default);

    /// <summary>
    /// Generates embedding vectors for multiple texts in batch.
    /// </summary>
    Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(IEnumerable<string> texts, CancellationToken ct = default);

    /// <summary>
    /// The dimensionality of the embedding vectors produced by this client.
    /// </summary>
    int Dimensions { get; }
}
