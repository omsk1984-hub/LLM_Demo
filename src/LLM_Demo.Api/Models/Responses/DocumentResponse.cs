namespace LLM_Demo.Api.Models.Responses;

/// <summary>
/// Response for document list and upload endpoints.
/// </summary>
public sealed record DocumentResponse(
    Guid Id,
    string Name,
    string ContentType,
    DateTime CreatedAt,
    int? ChunkCount = null);
