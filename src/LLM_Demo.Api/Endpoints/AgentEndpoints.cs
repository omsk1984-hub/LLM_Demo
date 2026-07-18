namespace LLM_Demo.Api.Endpoints;

using LLM_Demo.Api.Extensions;
using LLM_Demo.Api.Models.Requests;
using LLM_Demo.Api.Models.Responses;
using LLM_Demo.Domain.Agents;
using LLM_Demo.Infrastructure.Persistence.Repositories;

public sealed class AgentEndpoints
{
    private readonly AgentRepository _agentRepository;

    public AgentEndpoints(AgentRepository agentRepository)
    {
        _agentRepository = agentRepository;
    }

    public async Task<IResult> GetAll(HttpContext httpContext)
    {
        var userId = httpContext.GetUserId();
        var agents = await _agentRepository.GetByOwnerIdAsync(userId);
        return Results.Ok(agents);
    }

    public async Task<IResult> GetById(Guid id, HttpContext httpContext)
    {
        var agent = await _agentRepository.GetByIdAsync(id);
        if (agent is null)
            return Results.NotFound(new ErrorResponse("Agent not found"));

        return Results.Ok(agent);
    }

    public async Task<IResult> Create(CreateAgentRequest request, HttpContext httpContext)
    {
        var userId = httpContext.GetUserId();

        var agent = new Agent
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            SystemPrompt = request.SystemPrompt,
            OwnerId = userId,
            Status = AgentStatus.Idle
        };

        var created = await _agentRepository.AddAsync(agent);
        return Results.Created($"/api/agents/{created.Id}", created);
    }

    public async Task<IResult> Update(Guid id, UpdateAgentRequest request, HttpContext httpContext)
    {
        var agent = await _agentRepository.GetByIdAsync(id);
        if (agent is null)
            return Results.NotFound(new ErrorResponse("Agent not found"));

        if (request.Name is not null) agent.Name = request.Name;
        if (request.SystemPrompt is not null) agent.SystemPrompt = request.SystemPrompt;
        agent.UpdatedAt = DateTime.UtcNow;

        var updated = await _agentRepository.UpdateAsync(agent);
        return Results.Ok(updated);
    }

    public async Task<IResult> Delete(Guid id, HttpContext httpContext)
    {
        var deleted = await _agentRepository.DeleteAsync(id);
        if (!deleted)
            return Results.NotFound(new ErrorResponse("Agent not found"));

        return Results.NoContent();
    }
}
