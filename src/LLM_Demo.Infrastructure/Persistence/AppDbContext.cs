using System.Text.Json;
using LLM_Demo.Domain.Agents;
using LLM_Demo.Domain.Conversations;
using LLM_Demo.Domain.Messages;
using LLM_Demo.Domain.Tools;
using LLM_Demo.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace LLM_Demo.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Agent> Agents => Set<Agent>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<SubAgentReference> SubAgentReferences => Set<SubAgentReference>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("llm_demo");

        // ── Agent ──────────────────────────────────────────────
        modelBuilder.Entity<Agent>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Name).HasMaxLength(256).IsRequired();
            entity.Property(a => a.SystemPrompt).HasMaxLength(8192);
            entity.Property(a => a.OwnerId).HasMaxLength(128).IsRequired();
            entity.Property(a => a.Status).HasConversion<string>().HasMaxLength(64);
            entity.Property(a => a.CreatedAt);
            entity.Property(a => a.UpdatedAt);

            // Храним List<ToolDefinition> как JSON-колонку
            entity.OwnsMany(a => a.Tools, tools =>
            {
                tools.ToJson();
                tools.Property(t => t.Name).HasMaxLength(256);
                tools.Property(t => t.Description).HasMaxLength(2048);
            });
        });

        // ── Conversation ───────────────────────────────────────
        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Title).HasMaxLength(512).IsRequired();
            entity.Property(c => c.OwnerId).HasMaxLength(128).IsRequired();
            entity.Property(c => c.Status).HasConversion<string>().HasMaxLength(64);
            entity.Property(c => c.CreatedAt);
            entity.Property(c => c.UpdatedAt);
        });

        // ── Message ────────────────────────────────────────────
        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Role).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(m => m.Content).HasMaxLength(65535).IsRequired();
            entity.Property(m => m.Timestamp);
            entity.HasIndex(m => m.ConversationId);
        });

        // ── User ───────────────────────────────────────────────
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Username).HasMaxLength(128).IsRequired();
            entity.HasIndex(u => u.Username).IsUnique();
            entity.Property(u => u.Email).HasMaxLength(256).IsRequired();
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.PasswordHash).HasMaxLength(512).IsRequired();
            entity.Property(u => u.CreatedAt);
        });

        // ── RefreshToken ───────────────────────────────────────
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Token).HasMaxLength(512).IsRequired();
            entity.Property(r => r.ExpiresAt).IsRequired();
            entity.Property(r => r.CreatedAt);
            entity.Property(r => r.RevokedAt);
            entity.HasIndex(r => r.UserId);
            entity.HasIndex(r => r.Token).IsUnique();
        });

        // ── SubAgentReference ──────────────────────────────────
        modelBuilder.Entity<SubAgentReference>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Name).HasMaxLength(256).IsRequired();

            entity.HasOne<Agent>()
                  .WithMany()
                  .HasForeignKey(s => s.ParentAgentId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Agent>()
                  .WithMany()
                  .HasForeignKey(s => s.SubAgentId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(s => s.ParentAgentId);
            entity.HasIndex(s => s.SubAgentId);
        });
    }
}
