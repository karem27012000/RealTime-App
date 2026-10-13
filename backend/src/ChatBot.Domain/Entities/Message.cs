using ChatBot.Domain.Common;
using ChatBot.Domain.Enums;

namespace ChatBot.Domain.Entities;

public class Message : BaseEntity
{
    // Required by EF Core.
    private Message() { }

    internal Message(Guid conversationId, MessageRole role, string content)
    {
        ConversationId = conversationId;
        Role = role;
        Content = content;
    }

    public Guid ConversationId { get; private set; }

    public MessageRole Role { get; private set; }

    public string Content { get; private set; } = string.Empty;

    public Conversation? Conversation { get; private set; }
}
