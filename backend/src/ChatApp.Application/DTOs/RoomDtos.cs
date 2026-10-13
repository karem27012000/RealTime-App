namespace ChatApp.Application.DTOs;

/// <summary>
/// A room as seen by one user. For private chats, <see cref="Name"/> is the other user's
/// nickname and <see cref="OtherUserId"/> lets the UI show their online status.
/// </summary>
public sealed record RoomDto(
    Guid Id,
    string Name,
    string Type,
    Guid? OtherUserId,
    int MemberCount,
    MessageDto? LastMessage,
    DateTime UpdatedAt);

public sealed record RoomDetailsDto(
    Guid Id,
    string Name,
    string Type,
    Guid? OtherUserId,
    Guid CreatedById,
    IReadOnlyList<UserDto> Members);

public sealed record CreateGroupRequest(string Name);

public sealed record StartPrivateChatRequest(Guid UserId);
