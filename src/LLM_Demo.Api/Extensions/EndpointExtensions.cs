namespace LLM_Demo.Api.Extensions;

using LLM_Demo.Api.Endpoints;
using LLM_Demo.Api.Models.Responses;
using LLM_Demo.Domain.Agents;
using LLM_Demo.Domain.Connectors;
using LLM_Demo.Domain.Conversations;
using LLM_Demo.Domain.Messages;
using LLM_Demo.Domain.Tools;
using LLM_Demo.Infrastructure.Tools;

public static class EndpointExtensions
{
    public static RouteGroupBuilder MapAuthEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/login", async (AuthEndpoints endpoints, LLM_Demo.Api.Models.Requests.LoginRequest request) =>
            await endpoints.Login(request))
            .Produces<AuthResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized);

        group.MapPost("/register", async (AuthEndpoints endpoints, LLM_Demo.Api.Models.Requests.RegisterRequest request) =>
            await endpoints.Register(request))
            .Produces<AuthResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status409Conflict);

        return group;
    }

    public static RouteGroupBuilder MapAgentEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (AgentEndpoints endpoints, HttpContext http) =>
            await endpoints.GetAll(http))
            .Produces<List<Agent>>(StatusCodes.Status200OK);

        group.MapGet("/{id:guid}", async (Guid id, AgentEndpoints endpoints, HttpContext http) =>
            await endpoints.GetById(id, http))
            .Produces<Agent>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapPost("/", async (LLM_Demo.Api.Models.Requests.CreateAgentRequest request, AgentEndpoints endpoints, HttpContext http) =>
            await endpoints.Create(request, http))
            .Produces<Agent>(StatusCodes.Status201Created);

        group.MapPut("/{id:guid}", async (Guid id, LLM_Demo.Api.Models.Requests.UpdateAgentRequest request, AgentEndpoints endpoints, HttpContext http) =>
            await endpoints.Update(id, request, http))
            .Produces<Agent>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", async (Guid id, AgentEndpoints endpoints, HttpContext http) =>
            await endpoints.Delete(id, http))
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        return group;
    }

    public static RouteGroupBuilder MapConversationEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (ConversationEndpoints endpoints, HttpContext http) =>
            await endpoints.GetAll(http))
            .Produces<List<Conversation>>(StatusCodes.Status200OK);

        group.MapGet("/{id:guid}", async (Guid id, ConversationEndpoints endpoints, HttpContext http) =>
            await endpoints.GetById(id, http))
            .Produces<Conversation>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapPost("/", async (ConversationEndpoints endpoints, HttpContext http) =>
            await endpoints.Create(http))
            .Produces<Conversation>(StatusCodes.Status201Created);

        group.MapGet("/{id:guid}/messages", async (Guid id, ConversationEndpoints endpoints, HttpContext http) =>
            await endpoints.GetMessages(id, http))
            .Produces<List<Message>>(StatusCodes.Status200OK);

        return group;
    }

    public static RouteGroupBuilder MapChatEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{agentId:guid}", async (Guid agentId, LLM_Demo.Api.Models.Requests.ChatRequest request, ChatEndpoints endpoints, HttpContext http) =>
            await endpoints.Chat(agentId, request, http))
            .Produces(StatusCodes.Status202Accepted)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapGet("/{agentId:guid}/stream", async (Guid agentId, Guid conversationId, ChatEndpoints endpoints, HttpContext http) =>
            await endpoints.ChatStream(agentId, conversationId, http))
            .Produces(StatusCodes.Status200OK, contentType: "text/event-stream");

        return group;
    }

    public static RouteGroupBuilder MapToolEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", (IToolRegistry registry) =>
            Results.Ok(registry.GetAllTools()))
            .Produces<List<ToolDefinition>>(StatusCodes.Status200OK);

        return group;
    }

    public static RouteGroupBuilder MapConnectorEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", (IConnectorProvider provider) =>
            Results.Ok(provider.GetAvailableConnectors()))
            .Produces<IEnumerable<string>>(StatusCodes.Status200OK);

        return group;
    }

    public static RouteGroupBuilder MapDocumentEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{agentId:guid}/documents", async (Guid agentId, DocumentEndpoints endpoints, HttpContext http, CancellationToken ct) =>
            await endpoints.UploadDocument(agentId, http, ct))
            .Produces<DocumentResponse>(StatusCodes.Status201Created)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);

        group.MapGet("/{agentId:guid}/documents", async (Guid agentId, DocumentEndpoints endpoints, HttpContext http) =>
            await endpoints.ListDocuments(agentId, http))
            .Produces<List<DocumentResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapGet("/{agentId:guid}/documents/{documentId:guid}", async (Guid agentId, Guid documentId, DocumentEndpoints endpoints, HttpContext http) =>
            await endpoints.GetDocument(agentId, documentId, http))
            .Produces<DocumentDetailResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapDelete("/{agentId:guid}/documents/{documentId:guid}", async (Guid agentId, Guid documentId, DocumentEndpoints endpoints, HttpContext http) =>
            await endpoints.DeleteDocument(agentId, documentId, http))
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapGet("/{agentId:guid}/documents/search", async (Guid agentId, string query, DocumentEndpoints endpoints, HttpContext http, CancellationToken ct) =>
            await endpoints.SearchDocuments(agentId, query, http, ct))
            .Produces<List<SearchResultResponse>>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        return group;
    }
}
