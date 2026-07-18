namespace LLM_Demo.Api.Models.Requests;

public sealed record CreateAgentRequest(
    string Name,
    string SystemPrompt,
    string? ConnectorName = null,
    string? ModelId = null);

public sealed record UpdateAgentRequest(
    string? Name = null,
    string? SystemPrompt = null,
    string? ConnectorName = null,
    string? ModelId = null);
