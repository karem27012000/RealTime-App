using ChatBot.Application.Services;
using Microsoft.AspNetCore.SignalR;

namespace ChatBot.Api.Hubs;

/// <summary>
/// Real-time endpoint. The client calls <see cref="SendMessage"/> as a SignalR stream
/// and receives the bot's reply in small chunks while it is being generated.
/// </summary>
public sealed class ChatHub(IChatService chatService) : Hub
{
    public IAsyncEnumerable<string> SendMessage(Guid conversationId, string content, CancellationToken cancellationToken) =>
        chatService.StreamReplyAsync(conversationId, content, cancellationToken);
}
