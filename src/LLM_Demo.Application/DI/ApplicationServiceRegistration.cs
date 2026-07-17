namespace LLM_Demo.Application.DI;

using LLM_Demo.Application.Ownership;
using LLM_Demo.Application.SubAgents;
using Microsoft.Extensions.DependencyInjection;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Sub-agents
        services.AddSingleton<SubAgentOrchestrator>();
        services.AddSingleton<ISubAgentRouter, DefaultSubAgentRouter>();

        // Ownership
        services.AddSingleton<OwnershipService>();

        return services;
    }
}
