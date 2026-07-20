namespace LLM_Demo.Api.Models.Responses;

/// <summary>
/// Response for a single document with its chunks.
/// </summary>
public sealed record DocumentDetailResponse(
    Guid Id,
    string Name,
    string ContentType,
    DateTime CreatedAt,
    List<ChunkResponse> Chunks);
