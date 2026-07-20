namespace LLM_Demo.Infrastructure.Tools;

using LLM_Demo.Application.RAG;
using LLM_Demo.Domain.Tools;

/// <summary>
/// Tool that searches the agent's knowledge base for relevant documents
/// using vector similarity search (RAG).
/// </summary>
public sealed class RagTool
{
    private readonly VectorSearchService _vectorSearchService;

    public RagTool(VectorSearchService vectorSearchService)
    {
        _vectorSearchService = vectorSearchService;
    }

    public static ToolDefinition Definition => new()
    {
        Name = "search_documents",
        Description = "Searches the agent's knowledge base for relevant documents. " +
                      "Use this tool when you need specific information from uploaded documents " +
                      "to answer the user's question. Provide a search query describing what you're looking for. " +
                      "The tool returns relevant document excerpts with their similarity scores."
    };

    public async Task<ToolResult> ExecuteAsync(string arguments, Guid agentId)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(arguments);
            var query = doc.RootElement.GetProperty("query").GetString() ?? "";

            if (string.IsNullOrWhiteSpace(query))
                return ToolResult.Failure("search_documents requires a 'query' parameter.");

            var results = await _vectorSearchService.SearchAsync(agentId, query, topK: 5);

            if (results.Count == 0)
            {
                return ToolResult.Success("No relevant documents found for the query.");
            }

            var formatted = string.Join("\n\n---\n\n",
                results.Select((r, i) =>
                    $"[Result {i + 1}] (Score: {r.SimilarityScore:F2})\n{r.Content}"));

            return ToolResult.Success($"Found {results.Count} relevant document excerpts:\n\n{formatted}");
        }
        catch (System.Text.Json.JsonException ex)
        {
            return ToolResult.Failure($"Failed to parse arguments for search_documents: {ex.Message}");
        }
    }
}
