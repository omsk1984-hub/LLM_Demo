namespace LLM_Demo.Domain.Connectors;

using Microsoft.Extensions.AI;

public interface IConnectorProvider
{
    IChatClient GetClient(string connectorName);
    IEnumerable<string> GetAvailableConnectors();
    Task<bool> TestConnectionAsync(string connectorName, CancellationToken ct = default);
}
