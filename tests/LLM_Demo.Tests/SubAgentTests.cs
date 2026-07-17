namespace LLM_Demo.Tests.Application;

using FluentAssertions;
using LLM_Demo.Application.SubAgents;
using LLM_Demo.Domain.Agents;
using Microsoft.Extensions.Logging.Abstractions;

public sealed class SubAgentOrchestratorTests
{
    [Fact]
    public async Task ExecuteSubAgentsAsync_With_Empty_List_Should_Return_Empty()
    {
        var orchestrator = new SubAgentOrchestrator(NullLogger<SubAgentOrchestrator>.Instance);
        var result = await orchestrator.ExecuteSubAgentsAsync("test", []);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteSubAgentsAsync_With_Sequential_Strategy_Should_Return_Results()
    {
        var orchestrator = new SubAgentOrchestrator(NullLogger<SubAgentOrchestrator>.Instance);
        var subAgents = new List<SubAgentReference>
        {
            new() { Id = Guid.NewGuid(), Name = "Sub1", SubAgentId = Guid.NewGuid() },
            new() { Id = Guid.NewGuid(), Name = "Sub2", SubAgentId = Guid.NewGuid() }
        };

        var result = await orchestrator.ExecuteSubAgentsAsync(
            "test task", subAgents, SubAgentStrategy.Sequential);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("Sub1");
        result.Value.Should().Contain("Sub2");
    }
}

public sealed class DefaultSubAgentRouterTests
{
    [Fact]
    public void RouteTask_Should_Return_All_SubAgents()
    {
        var router = new DefaultSubAgentRouter();
        var subAgents = new List<SubAgentReference>
        {
            new() { Id = Guid.NewGuid(), Name = "Agent1" },
            new() { Id = Guid.NewGuid(), Name = "Agent2" }
        };

        var result = router.RouteTask("any task", subAgents);

        result.Should().HaveCount(2);
    }
}
