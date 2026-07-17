namespace LLM_Demo.Api.Models.Responses;

using LLM_Demo.Domain.Messages;

public sealed record ChatResponse(
    List<Message> Messages,
    int Iterations,
    TimeSpan Duration);
