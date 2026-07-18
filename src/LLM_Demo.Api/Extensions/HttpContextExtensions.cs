namespace LLM_Demo.Api.Extensions;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

/// <summary>
/// Extension methods for <see cref="HttpContext"/> to simplify common operations.
/// </summary>
public static class HttpContextExtensions
{
    /// <summary>
    /// Extracts the current user's ID from the JWT claims.
    /// Tries <c>sub</c> (JwtRegisteredClaimNames.Sub) first,
    /// then falls back to <c>ClaimTypes.NameIdentifier</c>.
    /// </summary>
    public static string GetUserId(this HttpContext httpContext)
    {
        return httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? string.Empty;
    }
}
