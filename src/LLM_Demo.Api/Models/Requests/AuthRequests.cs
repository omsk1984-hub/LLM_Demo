namespace LLM_Demo.Api.Models.Requests;

public sealed record RegisterRequest(
    string Username,
    string Email,
    string Password);

public sealed record LoginRequest(
    string Email,
    string Password);

public sealed record RefreshTokenRequest(
    string Token);
