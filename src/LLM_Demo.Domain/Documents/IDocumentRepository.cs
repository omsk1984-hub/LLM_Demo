namespace LLM_Demo.Domain.Documents;

/// <summary>
/// Repository interface for document and document chunk data access.
/// </summary>
public interface IDocumentRepository
{
    Task<Document?> GetByIdAsync(Guid documentId, CancellationToken ct = default);
    Task<IReadOnlyList<Document>> GetByAgentIdAsync(Guid agentId, CancellationToken ct = default);
    Task<Document?> GetWithChunksAsync(Guid documentId, CancellationToken ct = default);
    Task AddDocumentWithChunksAsync(Document document, IReadOnlyList<DocumentChunk> chunks, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid documentId, CancellationToken ct = default);
    Task<bool> DocumentBelongsToAgentAsync(Guid documentId, Guid agentId, CancellationToken ct = default);
    Task<IReadOnlyList<SearchDocumentChunk>> SearchChunksAsync(
        Guid agentId, float[] queryEmbedding, int topK = 5, CancellationToken ct = default);
}

/// <summary>
/// Result of a vector similarity search.
/// </summary>
public sealed class SearchDocumentChunk
{
    public Guid ChunkId { get; set; }
    public Guid DocumentId { get; set; }
    public string Content { get; set; } = string.Empty;
    public int ChunkIndex { get; set; }
    public double Distance { get; set; }
}
