namespace LLM_Demo.Api.Endpoints;

using LLM_Demo.Api.Extensions;
using LLM_Demo.Api.Models.Responses;
using LLM_Demo.Application.RAG;
using LLM_Demo.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Http.HttpResults;

public sealed class DocumentEndpoints
{
    private readonly AgentRepository _agentRepository;
    private readonly DocumentService _documentService;
    private readonly VectorSearchService _vectorSearchService;
    private readonly ILogger<DocumentEndpoints> _logger;

    public DocumentEndpoints(
        AgentRepository agentRepository,
        DocumentService documentService,
        VectorSearchService vectorSearchService,
        ILogger<DocumentEndpoints> logger)
    {
        _agentRepository = agentRepository;
        _documentService = documentService;
        _vectorSearchService = vectorSearchService;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/agents/{agentId}/documents — upload a document
    /// </summary>
    public async Task<IResult> UploadDocument(
        Guid agentId,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var userId = httpContext.GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Results.Unauthorized();

        var agent = await _agentRepository.GetByIdAsync(agentId);
        if (agent is null || agent.OwnerId != userId)
            return Results.NotFound(new ErrorResponse("Agent not found"));

        // Read document content from request body
        using var reader = new StreamReader(httpContext.Request.Body);
        var content = await reader.ReadToEndAsync(ct);

        if (string.IsNullOrWhiteSpace(content))
            return Results.BadRequest(new ErrorResponse("Document content cannot be empty"));

        // Try to get filename from Content-Disposition header or query
        var name = httpContext.Request.Query["name"].FirstOrDefault()
                   ?? $"document-{Guid.NewGuid():N}.txt";

        try
        {
            var document = await _documentService.UploadDocumentAsync(agentId, name, content, ct);
            return Results.Created($"/api/agents/{agentId}/documents/{document.Id}", new
            {
                document.Id,
                document.Name,
                document.ContentType,
                document.CreatedAt,
                ChunkCount = document.Chunks.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload document for agent {AgentId}", agentId);
            return Results.Problem("Failed to upload document");
        }
    }

    /// <summary>
    /// GET /api/agents/{agentId}/documents — list documents
    /// </summary>
    public async Task<IResult> ListDocuments(
        Guid agentId,
        HttpContext httpContext)
    {
        var userId = httpContext.GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Results.Unauthorized();

        var agent = await _agentRepository.GetByIdAsync(agentId);
        if (agent is null || agent.OwnerId != userId)
            return Results.NotFound(new ErrorResponse("Agent not found"));

        var documents = await _documentService.GetDocumentsAsync(agentId);
        return Results.Ok(documents.Select(d => new
        {
            d.Id,
            d.Name,
            d.ContentType,
            d.CreatedAt
        }));
    }

    /// <summary>
    /// GET /api/agents/{agentId}/documents/{documentId} — get document with chunks
    /// </summary>
    public async Task<IResult> GetDocument(
        Guid agentId,
        Guid documentId,
        HttpContext httpContext)
    {
        var userId = httpContext.GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Results.Unauthorized();

        var agent = await _agentRepository.GetByIdAsync(agentId);
        if (agent is null || agent.OwnerId != userId)
            return Results.NotFound(new ErrorResponse("Agent not found"));

        var document = await _documentService.GetDocumentWithChunksAsync(documentId);
        if (document is null)
            return Results.NotFound(new ErrorResponse("Document not found"));

        return Results.Ok(new
        {
            document.Id,
            document.Name,
            document.ContentType,
            document.CreatedAt,
            Chunks = document.Chunks.Select(c => new
            {
                c.Id,
                c.ChunkIndex,
                c.Content,
                HasEmbedding = c.Embedding is { Length: > 0 }
            })
        });
    }

    /// <summary>
    /// DELETE /api/agents/{agentId}/documents/{documentId} — delete a document
    /// </summary>
    public async Task<IResult> DeleteDocument(
        Guid agentId,
        Guid documentId,
        HttpContext httpContext)
    {
        var userId = httpContext.GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Results.Unauthorized();

        var agent = await _agentRepository.GetByIdAsync(agentId);
        if (agent is null || agent.OwnerId != userId)
            return Results.NotFound(new ErrorResponse("Agent not found"));

        var belongs = await _documentService.DocumentBelongsToAgentAsync(documentId, agentId);
        if (!belongs)
            return Results.NotFound(new ErrorResponse("Document not found"));

        var deleted = await _documentService.DeleteDocumentAsync(documentId);
        if (!deleted)
            return Results.NotFound(new ErrorResponse("Document not found"));

        return Results.NoContent();
    }

    /// <summary>
    /// POST /api/agents/{agentId}/documents/search — search documents using RAG
    /// </summary>
    public async Task<IResult> SearchDocuments(
        Guid agentId,
        string query,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var userId = httpContext.GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Results.Unauthorized();

        var agent = await _agentRepository.GetByIdAsync(agentId);
        if (agent is null || agent.OwnerId != userId)
            return Results.NotFound(new ErrorResponse("Agent not found"));

        if (string.IsNullOrWhiteSpace(query))
            return Results.BadRequest(new ErrorResponse("Query parameter is required"));

        var results = await _vectorSearchService.SearchAsync(agentId, query, topK: 10, ct);

        return Results.Ok(results.Select(r => new
        {
            r.ChunkId,
            r.DocumentId,
            r.Content,
            r.ChunkIndex,
            r.SimilarityScore
        }));
    }
}
