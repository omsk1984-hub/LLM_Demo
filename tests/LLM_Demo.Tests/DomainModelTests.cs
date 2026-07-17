namespace LLM_Demo.Tests.Domain;

using FluentAssertions;
using LLM_Demo.Domain.Agents;
using LLM_Demo.Domain.Conversations;
using LLM_Demo.Domain.Messages;
using LLM_Demo.Domain.Tools;

public sealed class DomainModelTests
{
    [Fact]
    public void Agent_Should_Have_Default_Status_Idle()
    {
        var agent = new Agent();
        agent.Status.Should().Be(AgentStatus.Idle);
    }

    [Fact]
    public void Agent_Should_Have_Default_Empty_Tools()
    {
        var agent = new Agent();
        agent.Tools.Should().BeEmpty();
    }

    [Fact]
    public void Conversation_Should_Have_Default_Status_Active()
    {
        var conversation = new Conversation();
        conversation.Status.Should().Be(ConversationStatus.Active);
    }

    [Fact]
    public void Message_Should_Set_Role_And_Content()
    {
        var message = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = Guid.NewGuid(),
            Role = MessageRole.User,
            Content = "Hello",
            Timestamp = DateTime.UtcNow
        };

        message.Role.Should().Be(MessageRole.User);
        message.Content.Should().Be("Hello");
    }

    [Fact]
    public void ToolResult_Success_Should_Have_IsSuccess_True()
    {
        var result = ToolResult.Success("OK");
        result.IsSuccess.Should().BeTrue();
        result.Result.Should().Be("OK");
    }

    [Fact]
    public void ToolResult_Failure_Should_Have_IsSuccess_False()
    {
        var result = ToolResult.Failure("Error occurred");
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Error occurred");
    }

    [Fact]
    public void AgentLoopResult_Success_Should_Contain_Messages()
    {
        var messages = new List<Message>
        {
            new() { Content = "Test", Role = MessageRole.Assistant }
        };

        var result = AgentLoopResult.Success(messages, 1, TimeSpan.FromSeconds(1));

        result.IsSuccess.Should().BeTrue();
        result.Messages.Should().HaveCount(1);
        result.Iterations.Should().Be(1);
    }

    [Fact]
    public void AgentLoopResult_Failure_Should_Have_Error()
    {
        var result = AgentLoopResult.Failure("Loop failed");
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Loop failed");
    }

    [Fact]
    public void StreamingChunk_Should_Support_Final_Flag()
    {
        var chunk = new StreamingChunk { Content = "Done", IsFinal = true };
        chunk.IsFinal.Should().BeTrue();
        chunk.Content.Should().Be("Done");
    }
}
