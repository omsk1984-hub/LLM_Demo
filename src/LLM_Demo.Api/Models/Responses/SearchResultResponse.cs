namespace LLM_Demo.Api.Models.Responses;

/// <summary>
/// Response item for a vector search result.
/// </summary>
public sealed record SearchResultResponse(
    Guid ChunkId,
    Guid DocumentId,
    string Content,
    int ChunkIndex,
    double SimilarityScore);
