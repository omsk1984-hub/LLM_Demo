namespace LLM_Demo.Infrastructure.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "LLM_Demo";
    public string Audience { get; set; } = "LLM_Demo_Api";
    public string SecretKey { get; set; } = string.Empty;
    public int ExpiryInMinutes { get; set; } = 60;
}
