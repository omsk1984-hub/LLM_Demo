namespace LLM_Demo.Application.RAG;

using LLM_Demo.Domain.Documents;
using LLM_Demo.Domain.RAG;
using Microsoft.Extensions.Logging;

/// <summary>
/// Performs vector similarity search over document chunks for a specific agent.
/// </summary>
public sealed class VectorSearchService
{
    private readonly IEmbeddingClient _embeddingClient;
    private readonly IDocumentRepository _documentRepository;
    private readonly ILogger<VectorSearchService> _logger;

    public VectorSearchService(
        IEmbeddingClient embeddingClient,
        IDocumentRepository documentRepository,
        ILogger<VectorSearchService> logger)
    {
        _embeddingClient = embeddingClient;
        _documentRepository = documentRepository;
        _logger = logger;
    }

    /// <summary>
    /// Searches for the most relevant document chunks for the given query.
    /// </summary>
    /// <param name="agentId">The agent whose documents to search.</param>
    /// <param name="query">The search query text.</param>
    /// <param name="topK">Maximum number of chunks to return (default: 5).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of relevant document chunks with similarity scores.</returns>
    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        Guid agentId,
        string query,
        int topK = 5,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<SearchResult>();

        // Generate embedding for the query
        var queryEmbedding = await _embeddingClient.GenerateEmbeddingAsync(query, ct);

        // Perform vector similarity search via repository
        var results = await _documentRepository.SearchChunksAsync(agentId, queryEmbedding, topK, ct);

        _logger.LogDebug(
            "Vector search for agent {AgentId} with query '{Query}' returned {Count} results",
            agentId, query[..Math.Min(query.Length, 50)], results.Count);

        return results.Select(r => new SearchResult
        {
            ChunkId = r.ChunkId,
            DocumentId = r.DocumentId,
            Content = r.Content,
            ChunkIndex = r.ChunkIndex,
            SimilarityScore = 1.0f - (float)r.Distance // convert cosine distance to similarity
        }).ToList();
    }

    public sealed class SearchResult
    {
        public Guid ChunkId { get; set; }
        public Guid DocumentId { get; set; }
        public string Content { get; set; } = string.Empty;
        public int ChunkIndex { get; set; }
        public float SimilarityScore { get; set; }
    }
}
