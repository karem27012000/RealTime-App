using ChatBot.Domain.Common;
using ChatBot.Domain.Enums;

namespace ChatBot.Domain.Entities;

/// <summary>
/// A chat session between the user and the bot. Messages can only be added
/// through <see cref="AddMessage"/> so the conversation stays consistent.
/// </summary>
public class Conversation : BaseEntity
{
    public const string DefaultTitle = "New chat";
    public const int TitleMaxLength = 100;

    private readonly List<Message> _messages = [];

    // Required by EF Core.
    private Conversation() { }

    public Conversation(string? title)
    {
        Title = NormalizeTitle(title);
        UpdatedAt = CreatedAt;
    }

    public string Title { get; private set; } = DefaultTitle;

    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyCollection<Message> Messages => _messages.AsReadOnly();

    public Message AddMessage(MessageRole role, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Message content cannot be empty.", nameof(content));
        }

        var message = new Message(Id, role, content.Trim());
        _messages.Add(message);
        UpdatedAt = message.CreatedAt;

        // Give untitled chats a title based on the first thing the user asked.
        if (role == MessageRole.User && Title == DefaultTitle)
        {
            Title = NormalizeTitle(message.Content);
        }

        return message;
    }

    public void Rename(string title) => Title = NormalizeTitle(title);

    private static string NormalizeTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return DefaultTitle;
        }

        var singleLine = title.Trim().ReplaceLineEndings(" ");
        return singleLine.Length <= TitleMaxLength
            ? singleLine
            : singleLine[..(TitleMaxLength - 3)] + "...";
    }
}
