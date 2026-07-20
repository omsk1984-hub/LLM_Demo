namespace LLM_Demo.Api.Models.Responses;

/// <summary>
/// Response item for a document chunk.
/// </summary>
public sealed record ChunkResponse(
    Guid Id,
    int ChunkIndex,
    string Content,
    bool HasEmbedding);
