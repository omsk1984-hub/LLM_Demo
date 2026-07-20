namespace LLM_Demo.Api.Extensions;

using LLM_Demo.Api.Endpoints;
using LLM_Demo.Domain.Connectors;
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

    public static RouteGroupBuilder MapConnectorEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", (IConnectorProvider provider) =>
            Results.Ok(provider.GetAvailableConnectors()));

        return group;
    }

    public static RouteGroupBuilder MapDocumentEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{agentId:guid}/documents", async (Guid agentId, DocumentEndpoints endpoints, HttpContext http, CancellationToken ct) =>
            await endpoints.UploadDocument(agentId, http, ct));

        group.MapGet("/{agentId:guid}/documents", async (Guid agentId, DocumentEndpoints endpoints, HttpContext http) =>
            await endpoints.ListDocuments(agentId, http));

        group.MapGet("/{agentId:guid}/documents/{documentId:guid}", async (Guid agentId, Guid documentId, DocumentEndpoints endpoints, HttpContext http) =>
            await endpoints.GetDocument(agentId, documentId, http));

        group.MapDelete("/{agentId:guid}/documents/{documentId:guid}", async (Guid agentId, Guid documentId, DocumentEndpoints endpoints, HttpContext http) =>
            await endpoints.DeleteDocument(agentId, documentId, http));

        group.MapGet("/{agentId:guid}/documents/search", async (Guid agentId, string query, DocumentEndpoints endpoints, HttpContext http, CancellationToken ct) =>
            await endpoints.SearchDocuments(agentId, query, http, ct));

        return group;
    }
}
