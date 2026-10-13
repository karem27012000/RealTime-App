using ChatBot.Domain.Enums;

namespace ChatBot.Application.Abstractions;

/// <summary>
/// One turn of the chat history that is sent to the AI model.
/// </summary>
public sealed record ChatTurn(MessageRole Role, string Content);

/// <summary>
/// The "brain" of the bot. The Application layer only knows this interface;
/// Infrastructure decides whether it is Claude or the offline demo bot.
/// </summary>
public interface IChatCompletionService
{
    /// <summary>
    /// Streams the bot's reply piece by piece so the UI can show it while it is being written.
    /// </summary>
    IAsyncEnumerable<string> StreamReplyAsync(
        IReadOnlyList<ChatTurn> history,
        CancellationToken cancellationToken = default);
}
