namespace LLM_Demo.Infrastructure.Persistence.Repositories;

using LLM_Demo.Domain.Documents;
using Microsoft.EntityFrameworkCore;
using Pgvector;

public sealed class DocumentRepository : IDocumentRepository
{
    private readonly AppDbContext _context;

    public DocumentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Document?> GetByIdAsync(Guid documentId, CancellationToken ct = default) =>
        await _context.Documents.FirstOrDefaultAsync(d => d.Id == documentId, ct);

    public async Task<IReadOnlyList<Document>> GetByAgentIdAsync(Guid agentId, CancellationToken ct = default) =>
        await _context.Documents
            .Where(d => d.AgentId == agentId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct);

    public async Task<Document?> GetWithChunksAsync(Guid documentId, CancellationToken ct = default) =>
        await _context.Documents
            .Include(d => d.Chunks.OrderBy(c => c.ChunkIndex))
            .FirstOrDefaultAsync(d => d.Id == documentId, ct);

    public async Task AddDocumentWithChunksAsync(
        Document document,
        IReadOnlyList<DocumentChunk> chunks,
        CancellationToken ct = default)
    {
        _context.Documents.Add(document);
        _context.DocumentChunks.AddRange(chunks);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAsync(Guid documentId, CancellationToken ct = default)
    {
        var document = await _context.Documents.FindAsync([documentId], cancellationToken: ct);
        if (document is null) return false;

        _context.Documents.Remove(document); // Cascade deletes chunks
        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DocumentBelongsToAgentAsync(Guid documentId, Guid agentId, CancellationToken ct = default) =>
        await _context.Documents.AnyAsync(d => d.Id == documentId && d.AgentId == agentId, ct);

    public async Task<IReadOnlyList<SearchDocumentChunk>> SearchChunksAsync(
        Guid agentId,
        float[] queryEmbedding,
        int topK = 5,
        CancellationToken ct = default)
    {
        // Use raw ADO.NET for vector similarity search with cosine distance (<->)
        // because Pgvector.EntityFrameworkCore v0.3.0 requires Npgsql 9.x which needs .NET 9.
        var connection = _context.Database.GetDbConnection();
        await connection.OpenAsync(ct);

        try
        {
            var vector = new Vector(queryEmbedding);
            var sql = @"
                SELECT 
                    dc.""Id"" AS ChunkId,
                    dc.""DocumentId"",
                    dc.""Content"",
                    dc.""ChunkIndex"",
                    dc.""Embedding"" <-> @queryVector::vector AS distance
                FROM llm_demo.""DocumentChunks"" dc
                INNER JOIN llm_demo.""Documents"" d ON d.""Id"" = dc.""DocumentId""
                WHERE d.""AgentId"" = @agentId
                  AND dc.""Embedding"" IS NOT NULL
                ORDER BY distance
                LIMIT @topK";

            await using var command = connection.CreateCommand();
            command.CommandText = sql;

            var queryVectorParam = command.CreateParameter();
            queryVectorParam.ParameterName = "@queryVector";
            queryVectorParam.Value = vector;
            command.Parameters.Add(queryVectorParam);

            var agentIdParam = command.CreateParameter();
            agentIdParam.ParameterName = "@agentId";
            agentIdParam.Value = agentId;
            command.Parameters.Add(agentIdParam);

            var topKParam = command.CreateParameter();
            topKParam.ParameterName = "@topK";
            topKParam.Value = topK;
            command.Parameters.Add(topKParam);

            var results = new List<SearchDocumentChunk>();
            await using var reader = await command.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                results.Add(new SearchDocumentChunk
                {
                    ChunkId = reader.GetGuid(0),
                    DocumentId = reader.GetGuid(1),
                    Content = reader.GetString(2),
                    ChunkIndex = reader.GetInt32(3),
                    Distance = reader.GetDouble(4)
                });
            }

            return results;
        }
        finally
        {
            await connection.CloseAsync();
        }
    }
}
