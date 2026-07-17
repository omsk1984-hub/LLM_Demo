namespace LLM_Demo.Infrastructure.Persistence.Repositories;

using LLM_Demo.Domain.Conversations;
using LLM_Demo.Domain.Messages;
using Microsoft.EntityFrameworkCore;

public sealed class ConversationRepository
{
    private readonly AppDbContext _context;

    public ConversationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Conversation?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _context.Conversations.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IEnumerable<Conversation>> GetAllAsync(CancellationToken ct = default) =>
        await _context.Conversations.ToListAsync(ct);

    public async Task<IEnumerable<Conversation>> GetByOwnerIdAsync(string ownerId, CancellationToken ct = default) =>
        await _context.Conversations.Where(c => c.OwnerId == ownerId).ToListAsync(ct);

    public async Task<Conversation> AddAsync(Conversation conversation, CancellationToken ct = default)
    {
        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync(ct);
        return conversation;
    }

    public async Task<Conversation> UpdateAsync(Conversation conversation, CancellationToken ct = default)
    {
        _context.Conversations.Update(conversation);
        await _context.SaveChangesAsync(ct);
        return conversation;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var conversation = await _context.Conversations.FindAsync([id, ct], cancellationToken: ct);
        if (conversation is null) return false;
        _context.Conversations.Remove(conversation);
        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IEnumerable<Message>> GetMessagesAsync(Guid conversationId, CancellationToken ct = default) =>
        await _context.Messages
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.Timestamp)
            .ToListAsync(ct);

    public async Task<Message> AddMessageAsync(Message message, CancellationToken ct = default)
    {
        _context.Messages.Add(message);
        await _context.SaveChangesAsync(ct);
        return message;
    }
}
