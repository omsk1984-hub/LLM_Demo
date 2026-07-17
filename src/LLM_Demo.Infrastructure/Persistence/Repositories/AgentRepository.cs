namespace LLM_Demo.Infrastructure.Persistence.Repositories;

using LLM_Demo.Domain.Agents;
using Microsoft.EntityFrameworkCore;

public sealed class AgentRepository
{
    private readonly AppDbContext _context;

    public AgentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Agent?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _context.Agents.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<IEnumerable<Agent>> GetAllAsync(CancellationToken ct = default) =>
        await _context.Agents.ToListAsync(ct);

    public async Task<IEnumerable<Agent>> GetByOwnerIdAsync(string ownerId, CancellationToken ct = default) =>
        await _context.Agents.Where(a => a.OwnerId == ownerId).ToListAsync(ct);

    public async Task<Agent> AddAsync(Agent agent, CancellationToken ct = default)
    {
        _context.Agents.Add(agent);
        await _context.SaveChangesAsync(ct);
        return agent;
    }

    public async Task<Agent> UpdateAsync(Agent agent, CancellationToken ct = default)
    {
        _context.Agents.Update(agent);
        await _context.SaveChangesAsync(ct);
        return agent;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var agent = await _context.Agents.FindAsync([id, ct], cancellationToken: ct);
        if (agent is null) return false;
        _context.Agents.Remove(agent);
        await _context.SaveChangesAsync(ct);
        return true;
    }
}
