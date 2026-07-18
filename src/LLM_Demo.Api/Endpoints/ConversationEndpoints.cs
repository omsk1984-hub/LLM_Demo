namespace LLM_Demo.Api.Endpoints;

using LLM_Demo.Api.Extensions;
using LLM_Demo.Api.Models.Responses;
using LLM_Demo.Domain.Conversations;
using LLM_Demo.Infrastructure.Persistence.Repositories;

public sealed class ConversationEndpoints
{
    private readonly ConversationRepository _conversationRepository;

    public ConversationEndpoints(ConversationRepository conversationRepository)
    {
        _conversationRepository = conversationRepository;
    }

    public async Task<IResult> GetAll(HttpContext httpContext)
    {
        var userId = httpContext.GetUserId();
        var conversations = await _conversationRepository.GetByOwnerIdAsync(userId);
        return Results.Ok(conversations);
    }

    public async Task<IResult> GetById(Guid id, HttpContext httpContext)
    {
        var conversation = await _conversationRepository.GetByIdAsync(id);
        if (conversation is null)
            return Results.NotFound(new ErrorResponse("Conversation not found"));

        return Results.Ok(conversation);
    }

    public async Task<IResult> Create(HttpContext httpContext)
    {
        var userId = httpContext.GetUserId();

        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            Title = $"Conversation {DateTime.UtcNow:yyyy-MM-dd HH:mm}",
            OwnerId = userId,
            Status = ConversationStatus.Active
        };

        var created = await _conversationRepository.AddAsync(conversation);
        return Results.Created($"/api/conversations/{created.Id}", created);
    }

    public async Task<IResult> GetMessages(Guid id, HttpContext httpContext)
    {
        var messages = await _conversationRepository.GetMessagesAsync(id);
        return Results.Ok(messages);
    }
}
