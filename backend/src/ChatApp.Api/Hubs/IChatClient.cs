using ChatApp.Application.DTOs;

namespace ChatApp.Api.Hubs;

/// <summary>
/// Methods the server can call on the browser. The names must match the
/// <c>connection.on('...')</c> handlers in the Angular ChatHubService.
/// </summary>
public interface IChatClient
{
    Task ReceiveMessage(MessageDto message);

    Task RoomAdded(RoomDto room);

    Task MembersChanged(Guid roomId, int memberCount);

    Task PresenceChanged(UserDto user);

    Task UserTyping(TypingDto typing);
}
