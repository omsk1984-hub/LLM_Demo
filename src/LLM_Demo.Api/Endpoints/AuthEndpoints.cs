namespace LLM_Demo.Api.Endpoints;

using LLM_Demo.Api.Models.Requests;
using LLM_Demo.Api.Models.Responses;
using LLM_Demo.Domain.Users;
using LLM_Demo.Infrastructure.Auth;
using LLM_Demo.Infrastructure.Persistence.Repositories;

public sealed class AuthEndpoints
{
    private readonly JwtTokenService _jwtService;
    private readonly UserRepository _userRepository;

    public AuthEndpoints(JwtTokenService jwtService, UserRepository userRepository)
    {
        _jwtService = jwtService;
        _userRepository = userRepository;
    }

    public async Task<IResult> Login(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest(new ErrorResponse("Email and password are required"));
        }

        var user = await _userRepository.GetByEmailAsync(request.Email.Trim().ToLowerInvariant());
        if (user is null)
        {
            return Results.Unauthorized();
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Results.Unauthorized();
        }

        var token = _jwtService.GenerateToken(user.Id.ToString(), ["user"]);

        return Results.Ok(new AuthResponse(
            token,
            user.Id.ToString(),
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

        if (await _userRepository.EmailExistsAsync(request.Email.Trim().ToLowerInvariant()))
        {
            return Results.Conflict(new ErrorResponse("Email already registered"));
        }

        if (await _userRepository.UsernameExistsAsync(request.Username.Trim()))
        {
            return Results.Conflict(new ErrorResponse("Username already taken"));
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user);

        var token = _jwtService.GenerateToken(user.Id.ToString(), ["user"]);

        return Results.Ok(new AuthResponse(
            token,
            user.Id.ToString(),
            DateTime.UtcNow.AddHours(1)));
    }
}
