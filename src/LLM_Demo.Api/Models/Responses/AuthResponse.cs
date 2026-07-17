namespace LLM_Demo.Api.Models.Responses;

public sealed record AuthResponse(
    string Token,
    string UserId,
    DateTime ExpiresAt);

public sealed record ErrorResponse(
    string Error,
    string? Detail = null);
