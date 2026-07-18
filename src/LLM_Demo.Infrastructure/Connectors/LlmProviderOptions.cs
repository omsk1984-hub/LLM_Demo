namespace LLM_Demo.Infrastructure.Connectors;

/// <summary>
/// Конфигурация одного LLM-провайдера из секции LLMProviders в appsettings.json.
/// </summary>
public sealed class LlmProviderOptions
{
    public const string SectionName = "LLMProviders";

    /// <summary>Базовый endpoint API (например "http://localhost:11434" или "https://api.openai.com/v1").</summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>Модель по умолчанию для этого провайдера (например "llama3.1", "gpt-4o-mini").</summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>API-ключ (если требуется).</summary>
    public string ApiKey { get; set; } = string.Empty;
}
