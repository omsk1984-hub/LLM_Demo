namespace LLM_Demo.Domain.Documents;

public sealed class Document
{
    public Guid Id { get; set; }
    public Guid AgentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ContentType { get; set; } = "text/plain";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
}
