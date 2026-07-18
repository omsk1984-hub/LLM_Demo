namespace LLM_Demo.Infrastructure.Persistence;

using LLM_Demo.Domain.Agents;
using LLM_Demo.Domain.Conversations;
using LLM_Demo.Domain.Messages;
using LLM_Demo.Domain.Users;
using Microsoft.EntityFrameworkCore;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        // Применяем миграции, если их нет
        await context.Database.MigrateAsync();

        // Если данные уже есть — не сидим
        if (await context.Users.AnyAsync())
            return;

        // ── Тестовый пользователь ──────────────────────────────
        var user = new User
        {
            Id = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
            Username = "demo_user",
            Email = "demo@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Demo123!"),
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);

        // ── Тестовые агенты ────────────────────────────────────
        var generalAgent = new Agent
        {
            Id = Guid.Parse("b1c2d3e4-f5a6-7890-abcd-ef1234567890"),
            Name = "General Assistant",
            SystemPrompt = "You are a helpful general-purpose assistant. Answer questions concisely and accurately.",
            Status = AgentStatus.Idle,
            OwnerId = user.Id.ToString(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Tools = []
        };

        var copywritingAgent = new Agent
        {
            Id = Guid.Parse("c1d2e3f4-a5b6-7890-abcd-ef1234567890"),
            Name = "Copywriting Assistant",
            SystemPrompt = "You are a professional copywriter. Help users write compelling marketing copy, emails, blog posts, and social media content.",
            Status = AgentStatus.Idle,
            OwnerId = user.Id.ToString(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Tools = []
        };

        var codeReviewAgent = new Agent
        {
            Id = Guid.Parse("d1e2f3a4-b5c6-7890-abcd-ef1234567890"),
            Name = "Code Reviewer",
            SystemPrompt = "You are a senior software engineer reviewing code. Provide constructive feedback on code quality, security, performance, and best practices.",
            Status = AgentStatus.Idle,
            OwnerId = user.Id.ToString(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Tools = []
        };

        context.Agents.AddRange(generalAgent, copywritingAgent, codeReviewAgent);

        // ── Тестовый диалог ────────────────────────────────────
        var conversation = new Conversation
        {
            Id = Guid.Parse("e1f2a3b4-c5d6-7890-abcd-ef1234567890"),
            Title = "Тестовый диалог",
            OwnerId = user.Id.ToString(),
            Status = ConversationStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Conversations.Add(conversation);

        // Сообщения в диалоге
        var messages = new List<Message>
        {
            new()
            {
                Id = Guid.Parse("f1a2b3c4-d5e6-7890-abcd-ef1234567890"),
                ConversationId = conversation.Id,
                Role = MessageRole.User,
                Content = "Привет! Расскажи, что ты умеешь?",
                Timestamp = DateTime.UtcNow.AddMinutes(-5)
            },
            new()
            {
                Id = Guid.Parse("a2b3c4d5-e6f7-7890-abcd-ef1234567890"),
                ConversationId = conversation.Id,
                Role = MessageRole.Assistant,
                Content = "Здравствуйте! Я — General Assistant. Я могу отвечать на вопросы, помогать с написанием текстов, проверять код и многое другое. Чем могу помочь?",
                Timestamp = DateTime.UtcNow.AddMinutes(-4)
            },
            new()
            {
                Id = Guid.Parse("b2c3d4e5-f6a7-7890-abcd-ef1234567890"),
                ConversationId = conversation.Id,
                Role = MessageRole.User,
                Content = "Отлично! Напиши приветственное письмо для новых клиентов.",
                Timestamp = DateTime.UtcNow.AddMinutes(-3)
            }
        };
        context.Messages.AddRange(messages);

        await context.SaveChangesAsync();
    }
}
