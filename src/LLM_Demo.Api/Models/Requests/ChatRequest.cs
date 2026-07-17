namespace LLM_Demo.Api.Models.Requests;

public sealed record ChatRequest(
    Guid ConversationId,
    string Message);
