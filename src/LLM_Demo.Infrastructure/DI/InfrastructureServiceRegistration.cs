namespace LLM_Demo.Infrastructure.DI;

using LLM_Demo.Infrastructure.Auth;
using LLM_Demo.Infrastructure.Persistence;
using LLM_Demo.Infrastructure.Persistence.Repositories;
using LLM_Demo.Infrastructure.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        // JWT
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<JwtTokenService>();

        // Tool Registry
        services.AddSingleton<IToolRegistry>(sp =>
        {
            var tools = sp.GetServices<ToolDefinition>();
            return new ToolRegistry(tools);
        });

        return services;
    }
}
