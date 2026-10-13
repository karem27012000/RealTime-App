using ChatApp.Application.Abstractions;

namespace ChatApp.Application.Tests.Fakes;

public sealed class FakePresenceTracker : IPresenceTracker
{
    private readonly Dictionary<Guid, HashSet<string>> _connections = [];

    public bool UserConnected(Guid userId, string connectionId)
    {
        if (!_connections.TryGetValue(userId, out var set))
        {
            _connections[userId] = set = [];
        }

        set.Add(connectionId);
        return set.Count == 1;
    }

    public bool UserDisconnected(Guid userId, string connectionId)
    {
        if (!_connections.TryGetValue(userId, out var set) || !set.Remove(connectionId))
        {
            return false;
        }

        return set.Count == 0;
    }

    public bool IsOnline(Guid userId) => _connections.TryGetValue(userId, out var set) && set.Count > 0;
}
