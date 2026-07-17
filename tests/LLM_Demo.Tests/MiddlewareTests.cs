namespace LLM_Demo.Tests.Middleware;

using FluentAssertions;
using LLM_Demo.Application.Middleware;
using LLM_Demo.Domain.Agents;
using LLM_Demo.Domain.Messages;
using LLM_Demo.Domain.Middleware;
using LLM_Demo.Domain.Tools;
using Microsoft.Extensions.Logging.Abstractions;

public sealed class FilteringMiddlewareTests
{
    [Fact]
    public async Task Should_Allow_Allowed_Tool()
    {
        var middleware = new FilteringMiddleware(NullLogger<FilteringMiddleware>.Instance);
        var context = new ToolMiddlewareContext
        {
            ToolCall = new ToolCall { Name = "calculator", Arguments = "{}" },
            Agent = new Agent
            {
                Tools = [new ToolDefinition { Name = "calculator" }]
            }
        };

        var result = await middleware.ExecuteAsync(context, _ =>
            Task.FromResult(ToolResult.Success("OK")));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Block_Not_Allowed_Tool()
    {
        var middleware = new FilteringMiddleware(NullLogger<FilteringMiddleware>.Instance);
        var context = new ToolMiddlewareContext
        {
            ToolCall = new ToolCall { Name = "dangerous_tool", Arguments = "{}" },
            Agent = new Agent
            {
                Tools = [new ToolDefinition { Name = "calculator" }]
            }
        };

        var result = await middleware.ExecuteAsync(context, _ =>
            Task.FromResult(ToolResult.Success("OK")));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not allowed");
    }
}

public sealed class SafetyMiddlewareTests
{
    [Fact]
    public async Task Should_Block_Dangerous_Pattern()
    {
        var middleware = new SafetyMiddleware(NullLogger<SafetyMiddleware>.Instance);
        var context = new ToolMiddlewareContext
        {
            ToolCall = new ToolCall { Name = "shell", Arguments = "rm -rf /" },
        };

        var result = await middleware.ExecuteAsync(context, _ =>
            Task.FromResult(ToolResult.Success("OK")));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Safety");
    }

    [Fact]
    public async Task Should_Allow_Safe_Pattern()
    {
        var middleware = new SafetyMiddleware(NullLogger<SafetyMiddleware>.Instance);
        var context = new ToolMiddlewareContext
        {
            ToolCall = new ToolCall { Name = "calculator", Arguments = "1 + 1" },
        };

        var result = await middleware.ExecuteAsync(context, _ =>
            Task.FromResult(ToolResult.Success("OK")));

        result.IsSuccess.Should().BeTrue();
    }
}

public sealed class LoggingMiddlewareTests
{
    [Fact]
    public async Task Should_Pass_Through_Success()
    {
        var middleware = new LoggingMiddleware(NullLogger<LoggingMiddleware>.Instance);
        var context = new ToolMiddlewareContext
        {
            ToolCall = new ToolCall { Name = "test_tool", Arguments = "{}" },
        };

        var result = await middleware.ExecuteAsync(context, _ =>
            Task.FromResult(ToolResult.Success("OK")));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Pass_Through_Failure()
    {
        var middleware = new LoggingMiddleware(NullLogger<LoggingMiddleware>.Instance);
        var context = new ToolMiddlewareContext
        {
            ToolCall = new ToolCall { Name = "test_tool", Arguments = "{}" },
        };

        var result = await middleware.ExecuteAsync(context, _ =>
            Task.FromResult(ToolResult.Failure("Error")));

        result.IsSuccess.Should().BeFalse();
    }
}
