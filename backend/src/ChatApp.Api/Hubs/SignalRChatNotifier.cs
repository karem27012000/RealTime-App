using ChatApp.Application.Abstractions;
using ChatApp.Application.DTOs;
using Microsoft.AspNetCore.SignalR;

namespace ChatApp.Api.Hubs;

/// <summary>
/// Implements the Application's <see cref="IChatNotifier"/> with SignalR, so services
/// (and REST controllers) can push events without knowing about SignalR.
/// </summary>
public sealed class SignalRChatNotifier(IHubContext<ChatHub, IChatClient> hub) : IChatNotifier
{
    public Task MessageReceivedAsync(MessageDto message, IEnumerable<Guid> recipientIds, CancellationToken cancellationToken = default) =>
        hub.Clients.Users(ToStrings(recipientIds)).ReceiveMessage(message);

    public Task RoomAddedAsync(Guid userId, RoomDto room, CancellationToken cancellationToken = default) =>
        hub.Clients.User(userId.ToString()).RoomAdded(room);

    public Task MembersChangedAsync(Guid roomId, int memberCount, IEnumerable<Guid> recipientIds, CancellationToken cancellationToken = default) =>
        hub.Clients.Users(ToStrings(recipientIds)).MembersChanged(roomId, memberCount);

    public Task PresenceChangedAsync(UserDto user, CancellationToken cancellationToken = default) =>
        hub.Clients.All.PresenceChanged(user);

    private static List<string> ToStrings(IEnumerable<Guid> ids) => ids.Select(id => id.ToString()).ToList();
}
