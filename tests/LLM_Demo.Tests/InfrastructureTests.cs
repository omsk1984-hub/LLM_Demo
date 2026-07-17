namespace LLM_Demo.Tests.Infrastructure;

using FluentAssertions;
using LLM_Demo.Domain.Agents;
using LLM_Demo.Domain.Tools;
using LLM_Demo.Infrastructure.Tools;
using Microsoft.Extensions.Logging.Abstractions;

public sealed class OwnershipServiceTests
{
    [Fact]
    public void IsOwner_Should_Return_True_When_OwnerId_Matches()
    {
        var service = new LLM_Demo.Application.Ownership.OwnershipService();

        var agent = new Agent { OwnerId = "user-123" };

        service.IsOwner(agent, "user-123").Should().BeTrue();
    }

    [Fact]
    public void IsOwner_Should_Return_False_When_OwnerId_Does_Not_Match()
    {
        var service = new LLM_Demo.Application.Ownership.OwnershipService();

        var agent = new Agent { OwnerId = "user-123" };

        service.IsOwner(agent, "other-user").Should().BeFalse();
    }
}

public sealed class SendSafetyToolTests
{
    [Fact]
    public async Task ExecuteAsync_Should_Block_Sensitive_Pattern()
    {
        var tool = new SendSafetyTool(NullLogger<SendSafetyTool>.Instance);
        var result = await tool.ExecuteAsync("password=secret123", "email@test.com");
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("sensitive");
    }

    [Fact]
    public async Task ExecuteAsync_Should_Allow_Safe_Content()
    {
        var tool = new SendSafetyTool(NullLogger<SendSafetyTool>.Instance);
        var result = await tool.ExecuteAsync("Hello, this is safe content", "user@test.com");
        result.IsSuccess.Should().BeTrue();
    }
}

public sealed class CalculatorToolTests
{
    [Fact]
    public void Execute_Simple_Addition_Should_Return_Correct_Result()
    {
        var tool = new CalculatorTool();
        var result = tool.Execute("2 + 2");
        result.IsSuccess.Should().BeTrue();
        result.Result.Should().Contain("4");
    }

    [Fact]
    public void Execute_Invalid_Expression_Should_Return_Failure()
    {
        var tool = new CalculatorTool();
        var result = tool.Execute("invalid++expression");
        result.IsSuccess.Should().BeFalse();
    }
}

public sealed class ToolRegistryTests
{
    [Fact]
    public void GetTool_Should_Return_Tool_By_Name()
    {
        var tools = new List<ToolDefinition>
        {
            new() { Name = "calculator", Description = "Calc" },
            new() { Name = "search", Description = "Search" }
        };

        var registry = new ToolRegistry(tools);
        var tool = registry.GetTool("calculator");

        tool.Should().NotBeNull();
        tool!.Name.Should().Be("calculator");
    }

    [Fact]
    public void GetTool_Should_Return_Null_For_Unknown()
    {
        var registry = new ToolRegistry([]);
        registry.GetTool("nonexistent").Should().BeNull();
    }

    [Fact]
    public void GetAllTools_Should_Return_All()
    {
        var tools = new List<ToolDefinition>
        {
            new() { Name = "tool1" },
            new() { Name = "tool2" }
        };

        var registry = new ToolRegistry(tools);
        registry.GetAllTools().Should().HaveCount(2);
    }
}
