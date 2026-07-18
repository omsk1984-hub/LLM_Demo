namespace LLM_Demo.Infrastructure.Persistence.Repositories;

using LLM_Demo.Domain.Users;
using Microsoft.EntityFrameworkCore;

public sealed class UserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _context.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        await _context.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default) =>
        await _context.Users.FirstOrDefaultAsync(u => u.Username == username, ct);

    public async Task<User> AddAsync(User user, CancellationToken ct = default)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync(ct);
        return user;
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken ct = default) =>
        await _context.Users.AnyAsync(u => u.Email == email, ct);

    public async Task<bool> UsernameExistsAsync(string username, CancellationToken ct = default) =>
        await _context.Users.AnyAsync(u => u.Username == username, ct);
}
