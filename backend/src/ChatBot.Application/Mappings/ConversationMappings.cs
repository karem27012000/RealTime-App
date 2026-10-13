using ChatBot.Application.DTOs;
using ChatBot.Domain.Entities;

namespace ChatBot.Application.Mappings;

/// <summary>
/// Converts entities to DTOs. Entities never leave the Application layer.
/// </summary>
public static class ConversationMappings
{
    public static ConversationDto ToDto(this Conversation conversation) =>
        new(conversation.Id, conversation.Title, conversation.CreatedAt, conversation.UpdatedAt);

    public static ConversationDetailsDto ToDetailsDto(this Conversation conversation) =>
        new(
            conversation.Id,
            conversation.Title,
            conversation.CreatedAt,
            conversation.UpdatedAt,
            conversation.Messages
                .OrderBy(m => m.CreatedAt)
                .Select(m => m.ToDto())
                .ToList());

    public static MessageDto ToDto(this Message message) =>
        new(message.Id, message.Role.ToString().ToLowerInvariant(), message.Content, message.CreatedAt);
}
