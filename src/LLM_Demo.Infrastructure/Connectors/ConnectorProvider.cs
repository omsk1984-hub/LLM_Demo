namespace LLM_Demo.Infrastructure.Connectors;

using LLM_Demo.Domain.Connectors;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

public sealed class ConnectorProvider : IConnectorProvider
{
    private readonly ILogger<ConnectorProvider> _logger;
    private readonly Dictionary<string, IChatClient> _clients;

    public ConnectorProvider(
        IEnumerable<KeyValuePair<string, IChatClient>> clients,
        ILogger<ConnectorProvider> logger)
    {
        _logger = logger;
        _clients = new Dictionary<string, IChatClient>(clients, StringComparer.OrdinalIgnoreCase);
    }

    public IChatClient GetClient(string connectorName)
    {
        if (_clients.TryGetValue(connectorName, out var client))
            return client;

        throw new KeyNotFoundException($"Connector '{connectorName}' not found. " +
            $"Available: {string.Join(", ", GetAvailableConnectors())}");
    }

    public IEnumerable<string> GetAvailableConnectors() =>
        _clients.Keys;

    public async Task<bool> TestConnectionAsync(string connectorName, CancellationToken ct = default)
    {
        try
        {
            var client = GetClient(connectorName);
            var messages = new List<ChatMessage>
            {
                new(ChatRole.User, "Respond with just 'ok'")
            };
            var response = await client.CompleteAsync(messages, cancellationToken: ct);
            return response is not null && !string.IsNullOrEmpty(response.Message?.Text);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Connection test failed for connector {ConnectorName}", connectorName);
            return false;
        }
    }
}
