namespace LLM_Demo.Api.Extensions;

using LLM_Demo.Api.Endpoints;
using LLM_Demo.Infrastructure.Tools;

public static class EndpointExtensions
{
    public static RouteGroupBuilder MapAuthEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/login", async (AuthEndpoints endpoints, LLM_Demo.Api.Models.Requests.LoginRequest request) =>
            await endpoints.Login(request));

        group.MapPost("/register", async (AuthEndpoints endpoints, LLM_Demo.Api.Models.Requests.RegisterRequest request) =>
            await endpoints.Register(request));

        return group;
    }

    public static RouteGroupBuilder MapAgentEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (AgentEndpoints endpoints, HttpContext http) =>
            await endpoints.GetAll(http));

        group.MapGet("/{id:guid}", async (Guid id, AgentEndpoints endpoints, HttpContext http) =>
            await endpoints.GetById(id, http));

        group.MapPost("/", async (LLM_Demo.Api.Models.Requests.CreateAgentRequest request, AgentEndpoints endpoints, HttpContext http) =>
            await endpoints.Create(request, http));

        group.MapPut("/{id:guid}", async (Guid id, LLM_Demo.Api.Models.Requests.UpdateAgentRequest request, AgentEndpoints endpoints, HttpContext http) =>
            await endpoints.Update(id, request, http));

        group.MapDelete("/{id:guid}", async (Guid id, AgentEndpoints endpoints, HttpContext http) =>
            await endpoints.Delete(id, http));

        return group;
    }

    public static RouteGroupBuilder MapConversationEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (ConversationEndpoints endpoints, HttpContext http) =>
            await endpoints.GetAll(http));

        group.MapGet("/{id:guid}", async (Guid id, ConversationEndpoints endpoints, HttpContext http) =>
            await endpoints.GetById(id, http));

        group.MapPost("/", async (ConversationEndpoints endpoints, HttpContext http) =>
            await endpoints.Create(http));

        group.MapGet("/{id:guid}/messages", async (Guid id, ConversationEndpoints endpoints, HttpContext http) =>
            await endpoints.GetMessages(id, http));

        return group;
    }

    public static RouteGroupBuilder MapChatEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{agentId:guid}", async (Guid agentId, LLM_Demo.Api.Models.Requests.ChatRequest request, ChatEndpoints endpoints, HttpContext http) =>
            await endpoints.Chat(agentId, request, http));

        group.MapGet("/{agentId:guid}/stream", async (Guid agentId, Guid conversationId, ChatEndpoints endpoints, HttpContext http) =>
            await endpoints.ChatStream(agentId, conversationId, http));

        return group;
    }

    public static RouteGroupBuilder MapToolEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", (IToolRegistry registry) =>
            Results.Ok(registry.GetAllTools()));

        return group;
    }
}
