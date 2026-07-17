namespace LLM_Demo.Application.Middleware;

using System.Collections.Concurrent;
using System.Threading.Channels;
using LLM_Demo.Domain.Messages;

/// <summary>
/// Manages SSE subscribers and broadcasts streaming chunks to them.
/// </summary>
public sealed class StreamingHandler
{
    private readonly ConcurrentDictionary<string, ChannelWriter<StreamingChunk>> _subscribers = new();

    public string Subscribe(ChannelWriter<StreamingChunk> writer)
    {
        var subscriptionId = Guid.NewGuid().ToString();
        _subscribers.TryAdd(subscriptionId, writer);
        return subscriptionId;
    }

    public void Unsubscribe(string subscriptionId)
    {
        if (_subscribers.TryRemove(subscriptionId, out var writer))
        {
            writer.TryComplete();
        }
    }

    public async Task BroadcastAsync(StreamingChunk chunk)
    {
        foreach (var (id, writer) in _subscribers)
        {
            try
            {
                await writer.WriteAsync(chunk);
            }
            catch
            {
                _subscribers.TryRemove(id, out _);
            }
        }
    }

    public int SubscriberCount => _subscribers.Count;
}
