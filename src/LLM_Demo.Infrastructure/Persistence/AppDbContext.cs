namespace LLM_Demo.Infrastructure.Persistence;

using LLM_Demo.Domain.Agents;
using LLM_Demo.Domain.Conversations;
using LLM_Demo.Domain.Messages;
using Microsoft.EntityFrameworkCore;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Agent> Agents => Set<Agent>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("llm_demo");

        modelBuilder.Entity<Agent>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Name).HasMaxLength(256).IsRequired();
            entity.Property(a => a.SystemPrompt).HasMaxLength(8192);
            entity.Property(a => a.OwnerId).HasMaxLength(128).IsRequired();
            entity.Property(a => a.Status).HasConversion<string>().HasMaxLength(64);
            entity.Property(a => a.CreatedAt);
            entity.Property(a => a.UpdatedAt);
        });

        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Title).HasMaxLength(512).IsRequired();
            entity.Property(c => c.OwnerId).HasMaxLength(128).IsRequired();
            entity.Property(c => c.Status).HasConversion<string>().HasMaxLength(64);
            entity.Property(c => c.CreatedAt);
            entity.Property(c => c.UpdatedAt);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Role).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(m => m.Content).HasMaxLength(65535).IsRequired();
            entity.Property(m => m.Timestamp);
            entity.HasIndex(m => m.ConversationId);
        });
    }
}
