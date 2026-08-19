using System.Collections.Concurrent;
using System.Threading.Channels;

namespace CampusHub.Notification.Api.Infrastructure;

/// <summary>
/// In-process pub/sub bus that allows the SSE stream endpoint to wake up
/// when a new in-app notification is stored for a given user.
/// </summary>
public sealed class NotificationBus
{
    private readonly ConcurrentDictionary<string, Channel<string>> _channels = new();

    public ChannelReader<string> Subscribe(string userId)
    {
        var ch = _channels.GetOrAdd(userId, _ =>
            Channel.CreateBounded<string>(new BoundedChannelOptions(32)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = false,
                SingleWriter = false,
            }));
        return ch.Reader;
    }

    public void Unsubscribe(string userId) => _channels.TryRemove(userId, out _);

    /// <summary>Notify all SSE listeners for this user. Non-blocking.</summary>
    public void Notify(string userId, string message)
    {
        if (_channels.TryGetValue(userId, out var ch))
        {
            ch.Writer.TryWrite(message);
        }
    }
}
