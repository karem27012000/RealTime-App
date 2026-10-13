namespace ChatBot.Application.DTOs;

public sealed record ConversationDto(
    Guid Id,
    string Title,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record ConversationDetailsDto(
    Guid Id,
    string Title,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<MessageDto> Messages);

public sealed record MessageDto(
    Guid Id,
    string Role,
    string Content,
    DateTime CreatedAt);

public sealed record CreateConversationRequest(string? Title);

public sealed record RenameConversationRequest(string Title);

public sealed record SendMessageRequest(string Content);
