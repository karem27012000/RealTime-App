namespace ChatApp.Application.Abstractions;

/// <summary>
/// Keeps track of who is online. A user can be connected from several tabs,
/// so we count connections and only report "online"/"offline" on the first/last one.
/// </summary>
public interface IPresenceTracker
{
    /// <returns>True if the user just came online (this is their first connection).</returns>
    bool UserConnected(Guid userId, string connectionId);

    /// <returns>True if the user just went offline (this was their last connection).</returns>
    bool UserDisconnected(Guid userId, string connectionId);

    bool IsOnline(Guid userId);
}
