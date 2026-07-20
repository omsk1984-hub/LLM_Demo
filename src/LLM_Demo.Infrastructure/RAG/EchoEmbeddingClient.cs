namespace LLM_Demo.Infrastructure.RAG;

using LLM_Demo.Domain.RAG;

/// <summary>
/// Fallback embedding client that returns zero-vectors when no embedding API is configured.
/// </summary>
internal sealed class EchoEmbeddingClient : IEmbeddingClient
{
    public int Dimensions => 1536;

    public Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct = default)
    {
        // Return a random-ish zero vector for demo purposes
        var vector = new float[Dimensions];
        var hash = text.GetHashCode();
        var rng = new Random(hash);
        for (var i = 0; i < Dimensions; i++)
            vector[i] = (float)(rng.NextDouble() * 2.0 - 1.0) * 0.01f;
        return Task.FromResult(vector);
    }

    public Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(
        IEnumerable<string> texts,
        CancellationToken ct = default)
    {
        var results = texts.Select(t => GenerateEmbeddingAsync(t, ct).Result).ToList();
        return Task.FromResult<IReadOnlyList<float[]>>(results);
    }
}
