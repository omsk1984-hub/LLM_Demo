namespace LLM_Demo.Infrastructure.Tools;

using LLM_Demo.Domain.Tools;
using Microsoft.Extensions.Logging;

/// <summary>
/// File system tool for reading and writing files.
/// Restricted to a sandbox directory for safety.
/// </summary>
public sealed class FileSystemTool
{
    private readonly string _sandboxPath;
    private readonly ILogger<FileSystemTool> _logger;

    public FileSystemTool(ILogger<FileSystemTool> logger)
    {
        _sandboxPath = Path.Combine(AppContext.BaseDirectory, "sandbox");
        Directory.CreateDirectory(_sandboxPath);
        _logger = logger;
    }

    public static ToolDefinition Definition => new()
    {
        Name = "file_system",
        Description = "Reads or writes files in the sandbox directory. " +
                      "Actions: read, write, list, delete."
    };

    public async Task<ToolResult> ExecuteAsync(string action, string filename, string? content = null)
    {
        // Security: prevent path traversal
        var safeName = Path.GetFileName(filename);
        var fullPath = Path.Combine(_sandboxPath, safeName);

        if (!fullPath.StartsWith(_sandboxPath, StringComparison.Ordinal))
        {
            _logger.LogWarning("Path traversal attempt blocked: {Filename}", filename);
            return ToolResult.Failure("Access denied: path traversal detected.");
        }

        return action.ToLowerInvariant() switch
        {
            "read" => await ReadFileAsync(fullPath),
            "write" => await WriteFileAsync(fullPath, content ?? ""),
            "list" => ListFiles(),
            "delete" => DeleteFile(fullPath),
            _ => ToolResult.Failure($"Unknown action '{action}'. Use: read, write, list, delete")
        };
    }

    private async Task<ToolResult> ReadFileAsync(string path)
    {
        if (!File.Exists(path))
            return ToolResult.Failure($"File not found: {Path.GetFileName(path)}");

        var content = await File.ReadAllTextAsync(path);
        return ToolResult.Success(content);
    }

    private async Task<ToolResult> WriteFileAsync(string path, string content)
    {
        await File.WriteAllTextAsync(path, content);
        _logger.LogInformation("File written: {Path}", path);
        return ToolResult.Success($"Written {content.Length} chars to {Path.GetFileName(path)}");
    }

    private ToolResult ListFiles()
    {
        var files = Directory.GetFiles(_sandboxPath)
            .Select(f => Path.GetFileName(f));
        return ToolResult.Success(string.Join("\n", files));
    }

    private ToolResult DeleteFile(string path)
    {
        if (!File.Exists(path))
            return ToolResult.Failure($"File not found: {Path.GetFileName(path)}");

        File.Delete(path);
        return ToolResult.Success($"Deleted {Path.GetFileName(path)}");
    }
}
