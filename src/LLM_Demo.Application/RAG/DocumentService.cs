namespace LLM_Demo.Application.RAG;

using LLM_Demo.Domain.Documents;
using LLM_Demo.Domain.RAG;
using Microsoft.Extensions.Logging;

/// <summary>
/// Handles document upload, chunking, embedding, and storage.
/// </summary>
public sealed class DocumentService
{
    private readonly IEmbeddingClient _embeddingClient;
    private readonly IDocumentRepository _documentRepository;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(
        IEmbeddingClient embeddingClient,
        IDocumentRepository documentRepository,
        ILogger<DocumentService> logger)
    {
        _embeddingClient = embeddingClient;
        _documentRepository = documentRepository;
        _logger = logger;
    }

    /// <summary>
    /// Uploads a text document, splits it into chunks, generates embeddings, and stores everything.
    /// </summary>
    /// <param name="agentId">The agent this document belongs to.</param>
    /// <param name="name">Document name/filename.</param>
    /// <param name="content">Full text content of the document.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created document.</returns>
    public async Task<Document> UploadDocumentAsync(
        Guid agentId,
        string name,
        string content,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Document content cannot be empty", nameof(content));

        // 1. Split document into chunks
        var chunks = ChunkText(content);

        _logger.LogInformation(
            "Uploading document '{Name}' for agent {AgentId}: {TotalChars} chars -> {ChunkCount} chunks",
            name, agentId, content.Length, chunks.Count);

        // 2. Generate embeddings for all chunks in batch
        var embeddings = await _embeddingClient.GenerateEmbeddingsAsync(chunks.Select(c => c.Text), ct);

        // 3. Create domain entities
        var documentId = Guid.NewGuid();
        var document = new Document
        {
            Id = documentId,
            AgentId = agentId,
            Name = name,
            ContentType = "text/plain",
            CreatedAt = DateTime.UtcNow
        };

        var documentChunks = chunks.Select((chunk, i) => new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            Content = chunk.Text,
            Embedding = embeddings.Count > i ? embeddings[i] : null,
            ChunkIndex = i
        }).ToList();

        // 4. Save to database via repository
        await _documentRepository.AddDocumentWithChunksAsync(document, documentChunks, ct);

        _logger.LogInformation("Document '{Name}' saved with {ChunkCount} chunks", name, documentChunks.Count);

        return document;
    }

    /// <summary>
    /// Returns all documents for a given agent.
    /// </summary>
    public async Task<IReadOnlyList<Document>> GetDocumentsAsync(
        Guid agentId,
        CancellationToken ct = default)
    {
        return await _documentRepository.GetByAgentIdAsync(agentId, ct);
    }

    /// <summary>
    /// Returns a document with its chunks.
    /// </summary>
    public async Task<Document?> GetDocumentWithChunksAsync(
        Guid documentId,
        CancellationToken ct = default)
    {
        return await _documentRepository.GetWithChunksAsync(documentId, ct);
    }

    /// <summary>
    /// Deletes a document and all its chunks.
    /// </summary>
    public async Task<bool> DeleteDocumentAsync(
        Guid documentId,
        CancellationToken ct = default)
    {
        return await _documentRepository.DeleteAsync(documentId, ct);
    }

    /// <summary>
    /// Checks if a document belongs to a specific agent.
    /// </summary>
    public async Task<bool> DocumentBelongsToAgentAsync(
        Guid documentId,
        Guid agentId,
        CancellationToken ct = default)
    {
        return await _documentRepository.DocumentBelongsToAgentAsync(documentId, agentId, ct);
    }

    // ── Chunking logic ──────────────────────────────────────────

    /// <summary>
    /// Splits text into overlapping chunks of approximately maxTokens tokens.
    /// Uses a simple character-based heuristic (~4 chars per token).
    /// </summary>
    private static List<Chunk> ChunkText(string text, int maxTokens = 500, int overlapTokens = 50)
    {
        var chunks = new List<Chunk>();
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length > 0)
        {
            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    chunks.Add(new Chunk(chunks.Count, line.Trim()));
                }
            }
            return chunks;
        }
        return chunks;
    }

    private sealed record Chunk(int Index, string Text);
}
