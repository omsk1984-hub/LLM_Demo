namespace LLM_Demo.Domain.Agents;

using LLM_Demo.Domain.Conversations;
using LLM_Demo.Domain.Messages;
using LLM_Demo.Domain.Tools;

public sealed class Agent : LLM_Demo.Domain.Ownership.IOwnable
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = string.Empty;
    public AgentStatus Status { get; set; } = AgentStatus.Idle;
    public string OwnerId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<ToolDefinition> Tools { get; set; } = [];

    /// <summary>
    /// Имя LLM-провайдера (например "default", "openai", "ollama").
    /// Должно соответствовать ключу в секции LLMProviders конфигурации.
    /// </summary>
    public string ConnectorName { get; set; } = "default";

    /// <summary>
    /// Идентификатор модели (например "gpt-4o-mini", "llama3.1", "qwen2.5").
    /// Передаётся в LLM-провайдер при каждом запросе.
    /// </summary>
    public string ModelId { get; set; } = string.Empty;
}
