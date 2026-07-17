namespace LLM_Demo.Api.Models.Requests;

public sealed record CreateAgentRequest(
    string Name,
    string SystemPrompt);

public sealed record UpdateAgentRequest(
    string? Name,
    string? SystemPrompt);
