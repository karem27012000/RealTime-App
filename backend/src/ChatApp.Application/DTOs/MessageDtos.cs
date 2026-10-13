namespace ChatApp.Application.DTOs;

public sealed record MessageDto(
    Guid Id,
    Guid RoomId,
    Guid SenderId,
    string SenderNickname,
    string Content,
    DateTime CreatedAt);

public sealed record SendMessageRequest(string Content);

public sealed record TypingDto(Guid RoomId, Guid UserId, string Nickname);
