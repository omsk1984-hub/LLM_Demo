namespace LLM_Demo.Api.Endpoints;

using LLM_Demo.Api.Models.Requests;
using LLM_Demo.Api.Models.Responses;
using LLM_Demo.Infrastructure.Auth;

public sealed class AuthEndpoints
{
    private readonly JwtTokenService _jwtService;

    public AuthEndpoints(JwtTokenService jwtService)
    {
        _jwtService = jwtService;
    }

    public async Task<IResult> Login(LoginRequest request)
    {
        // In production: validate credentials against DB
        // For demo: accept any non-empty email/password
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest(new ErrorResponse("Invalid credentials"));
        }

        var userId = Guid.NewGuid().ToString();
        var token = _jwtService.GenerateToken(userId, ["user"]);

        return Results.Ok(new AuthResponse(
            token,
            userId,
            DateTime.UtcNow.AddHours(1)));
    }

    public async Task<IResult> Register(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.Username))
        {
            return Results.BadRequest(new ErrorResponse("All fields are required"));
        }

        var userId = Guid.NewGuid().ToString();
        var token = _jwtService.GenerateToken(userId, ["user"]);

        return Results.Ok(new AuthResponse(
            token,
            userId,
            DateTime.UtcNow.AddHours(1)));
    }
}
