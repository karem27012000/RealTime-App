namespace ChatApp.Application.DTOs;

public sealed record UserDto(Guid Id, string Nickname, bool IsOnline, DateTime LastSeenAt);

public sealed record LoginRequest(string Nickname);
