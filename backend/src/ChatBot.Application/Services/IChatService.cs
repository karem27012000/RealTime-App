using ChatBot.Application.DTOs;

namespace ChatBot.Application.Services;

public interface IChatService
{
    /// <summary>
    /// Saves the user's message and streams the bot's reply chunk by chunk (used by the SignalR hub).
    /// </summary>
    IAsyncEnumerable<string> StreamReplyAsync(Guid conversationId, string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the user's message and returns the complete bot reply (used by the REST endpoint).
    /// </summary>
    Task<MessageDto> SendMessageAsync(Guid conversationId, string content, CancellationToken cancellationToken = default);
}
