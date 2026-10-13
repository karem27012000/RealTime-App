using ChatApp.Application.Abstractions;
using ChatApp.Application.DTOs;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;

namespace ChatApp.Application.Mappings;

/// <summary>
/// Converts entities to DTOs. Entities never leave the Application layer.
/// </summary>
public static class ChatMappings
{
    public static UserDto ToDto(this User user, IPresenceTracker presence) =>
        new(user.Id, user.Nickname, presence.IsOnline(user.Id), user.LastSeenAt);

    public static MessageDto ToDto(this Message message, string senderNickname) =>
        new(message.Id, message.RoomId, message.SenderId, senderNickname, message.Content, message.CreatedAt);

    public static MessageDto ToDto(this Message message) =>
        message.ToDto(message.Sender?.Nickname ?? "Unknown");

    /// <param name="viewerId">The user who will see this room (decides the name of private chats).</param>
    public static RoomDto ToDto(this ChatRoom room, Guid viewerId, Message? lastMessage = null)
    {
        var other = room.Type == RoomType.Private
            ? room.Members.FirstOrDefault(m => m.UserId != viewerId)
            : null;

        return new RoomDto(
            room.Id,
            DisplayName(room, other),
            room.Type.ToString().ToLowerInvariant(),
            other?.UserId,
            room.Members.Count,
            lastMessage?.ToDto(),
            room.UpdatedAt);
    }

    public static RoomDetailsDto ToDetailsDto(this ChatRoom room, Guid viewerId, IPresenceTracker presence)
    {
        var other = room.Type == RoomType.Private
            ? room.Members.FirstOrDefault(m => m.UserId != viewerId)
            : null;

        return new RoomDetailsDto(
            room.Id,
            DisplayName(room, other),
            room.Type.ToString().ToLowerInvariant(),
            other?.UserId,
            room.CreatedById,
            room.Members
                .Where(m => m.User is not null)
                .Select(m => m.User!.ToDto(presence))
                .OrderBy(u => u.Nickname)
                .ToList());
    }

    private static string DisplayName(ChatRoom room, RoomMember? otherMember) =>
        room.Type == RoomType.Group
            ? room.Name ?? "Group"
            : otherMember?.User?.Nickname ?? "Private chat";
}
