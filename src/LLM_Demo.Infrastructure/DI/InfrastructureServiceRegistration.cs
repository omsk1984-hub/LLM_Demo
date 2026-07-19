namespace LLM_Demo.Infrastructure.DI;

using LLM_Demo.Domain.Connectors;
using LLM_Demo.Domain.Tools;
using LLM_Demo.Infrastructure.Auth;
using LLM_Demo.Infrastructure.Connectors;
using LLM_Demo.Infrastructure.Persistence;
using LLM_Demo.Infrastructure.Persistence.Repositories;
using LLM_Demo.Infrastructure.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // EF Core + PostgreSQL
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        // Repositories
        services.AddScoped<AgentRepository>();
        services.AddScoped<ConversationRepository>();
        services.AddScoped<UserRepository>();

        // JWT
        var jwtSection = configuration.GetSection(JwtOptions.SectionName);
        var jwtOptions = new JwtOptions
        {
            Issuer = jwtSection["Issuer"] ?? "LLM_Demo",
            Audience = jwtSection["Audience"] ?? "LLM_Demo_Api",
            SecretKey = jwtSection["SecretKey"] ?? "default-dev-key-please-change-in-production",
            ExpiryInMinutes = int.TryParse(jwtSection["ExpiryInMinutes"], out var expiry) ? expiry : 60
        };
        services.AddSingleton(jwtOptions);
        services.AddSingleton<JwtTokenService>();

        // Tool Registry
        services.AddSingleton<IToolRegistry>(sp =>
        {
            var tools = sp.GetServices<ToolDefinition>();
            return new ToolRegistry(tools);
        });

        // LLM Providers — регистрируем ConnectorProvider как singleton
        services.AddSingleton<IConnectorProvider>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<ConnectorProvider>>();

            var providers = new Dictionary<string, IChatClient>(StringComparer.OrdinalIgnoreCase);

            var llmSection = configuration.GetSection(LlmProviderOptions.SectionName);
            logger.LogInformation("LLMProviders section loaded: {SectionPath} with {Count} child providers",
                llmSection.Path, llmSection.GetChildren().Count());
            foreach (var child in llmSection.GetChildren())
            {
                var providerName = child.Key;
                var endpoint = child["Endpoint"] ?? string.Empty;
                var modelId = child["ModelId"] ?? string.Empty;
                var apiKey = child["ApiKey"] ?? string.Empty;

                if (string.IsNullOrWhiteSpace(endpoint))
                    continue;

                // Создаём HttpClient с bypass SSL (для routerai.ru и других кастомных endpoint'ов),
                // как в test2/Test2.Console/Program.cs
                var httpClient = new HttpClient(new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (_, _, _, _) => true
                })
                {
                    Timeout = TimeSpan.FromMinutes(5)
                };

                var chatClient = OpenAIConnectorFactory.CreateHttpChatClient(
                    httpClient, endpoint, modelId, string.IsNullOrWhiteSpace(apiKey) ? null : apiKey);

                providers[providerName] = chatClient;
            }

            // Добавляем EchoChatClient как fallback, если провайдеров нет
            if (providers.Count == 0)
            {
                providers["default"] = new EchoChatClient();
            }

            return new ConnectorProvider(providers, logger);
        });

        return services;
    }
}
