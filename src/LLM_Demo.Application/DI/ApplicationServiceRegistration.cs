namespace LLM_Demo.Application.DI;

using LLM_Demo.Application.Middleware;
using LLM_Demo.Application.Ownership;
using LLM_Demo.Application.SubAgents;
using LLM_Demo.Domain.Middleware;
using LLM_Demo.Domain.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Sub-agents
        services.AddSingleton<SubAgentOrchestrator>();
        services.AddSingleton<ISubAgentRouter, DefaultSubAgentRouter>();

        // Ownership
        services.AddSingleton<OwnershipService>();

        // StreamingHandler — для SSE-трансляции вызовов инструментов
        services.AddSingleton<StreamingHandler>();

        // Tool Middleware (порядок регистрации = порядок выполнения)
        services.AddSingleton<IToolMiddleware, LoggingMiddleware>();
        services.AddSingleton<IToolMiddleware, FilteringMiddleware>();
        services.AddSingleton<IToolMiddleware, SafetyMiddleware>();
        services.AddSingleton<IToolMiddleware, StreamingMiddleware>();

        // ToolMiddlewarePipeline — собирает цепочку middleware + dispatcher
        services.AddSingleton<ToolMiddlewarePipeline>(sp =>
        {
            var middlewares = sp.GetServices<IToolMiddleware>();
            var logger = sp.GetRequiredService<ILogger<ToolMiddlewarePipeline>>();

            // coreHandler использует IToolDispatcher из Infrastructure
            Func<ToolMiddlewareContext, Task<ToolResult>> coreHandler = async ctx =>
            {
                var dispatcher = sp.GetRequiredService<IToolDispatcher>();
                return await dispatcher.ExecuteAsync(ctx);
            };

            return new ToolMiddlewarePipeline(middlewares, coreHandler, logger);
        });

        return services;
    }
}
