using System.Collections.Concurrent;
using ChatApp.Application.Abstractions;

namespace ChatApp.Infrastructure.Presence;

/// <summary>
/// Remembers which SignalR connections each user has. Stored in memory, so it is
/// registered as a singleton and resets when the server restarts.
/// (With several servers you would store this in Redis instead.)
/// </summary>
public sealed class InMemoryPresenceTracker : IPresenceTracker
{
    private readonly ConcurrentDictionary<Guid, HashSet<string>> _connections = new();

    public bool UserConnected(Guid userId, string connectionId)
    {
        var set = _connections.GetOrAdd(userId, _ => []);
        lock (set)
        {
            set.Add(connectionId);
            return set.Count == 1;
        }
    }

    public bool UserDisconnected(Guid userId, string connectionId)
    {
        if (!_connections.TryGetValue(userId, out var set))
        {
            return false;
        }

        lock (set)
        {
            set.Remove(connectionId);
            if (set.Count > 0)
            {
                return false;
            }

            _connections.TryRemove(userId, out _);
            return true;
        }
    }

    public bool IsOnline(Guid userId) =>
        _connections.TryGetValue(userId, out var set) && set.Count > 0;
}
