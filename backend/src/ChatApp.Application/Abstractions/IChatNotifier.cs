using ChatApp.Application.DTOs;

namespace ChatApp.Application.Abstractions;

/// <summary>
/// Pushes real-time events to connected users. The Application layer only knows this
/// interface; the Api layer implements it with SignalR.
/// </summary>
public interface IChatNotifier
{
    Task MessageReceivedAsync(MessageDto message, IEnumerable<Guid> recipientIds, CancellationToken cancellationToken = default);

    Task RoomAddedAsync(Guid userId, RoomDto room, CancellationToken cancellationToken = default);

    Task MembersChangedAsync(Guid roomId, int memberCount, IEnumerable<Guid> recipientIds, CancellationToken cancellationToken = default);

    Task PresenceChangedAsync(UserDto user, CancellationToken cancellationToken = default);
}
