using System.Runtime.CompilerServices;
using System.Text;
using ChatBot.Application.Abstractions;
using ChatBot.Application.DTOs;
using ChatBot.Application.Exceptions;
using ChatBot.Application.Mappings;
using ChatBot.Domain.Entities;
using ChatBot.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace ChatBot.Application.Services;

public sealed class ChatService(
    IConversationRepository repository,
    IChatCompletionService chatCompletion,
    ILogger<ChatService> logger) : IChatService
{
    public const int MaxMessageLength = 4000;

    public async IAsyncEnumerable<string> StreamReplyAsync(
        Guid conversationId,
        string content,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ValidateContent(content);

        var conversation = await repository.GetWithMessagesAsync(conversationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Conversation), conversationId);

        conversation.AddMessage(MessageRole.User, content);
        await repository.SaveChangesAsync(cancellationToken);

        var history = conversation.Messages
            .OrderBy(m => m.CreatedAt)
            .Select(m => new ChatTurn(m.Role, m.Content))
            .ToList();

        var reply = new StringBuilder();
        try
        {
            await foreach (var chunk in chatCompletion.StreamReplyAsync(history, cancellationToken))
            {
                reply.Append(chunk);
                yield return chunk;
            }
        }
        finally
        {
            // Save whatever the bot wrote, even if the user disconnected half way.
            if (reply.Length > 0)
            {
                conversation.AddMessage(MessageRole.Assistant, reply.ToString());
                await repository.SaveChangesAsync(CancellationToken.None);
            }
            else
            {
                logger.LogWarning("The bot returned an empty reply for conversation {ConversationId}", conversationId);
            }
        }
    }

    public async Task<MessageDto> SendMessageAsync(Guid conversationId, string content, CancellationToken cancellationToken = default)
    {
        await foreach (var _ in StreamReplyAsync(conversationId, content, cancellationToken))
        {
            // Consume the whole stream; the reply is saved when it completes.
        }

        var conversation = await repository.GetWithMessagesAsync(conversationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Conversation), conversationId);

        var lastMessage = conversation.Messages.OrderBy(m => m.CreatedAt).Last();
        return lastMessage.ToDto();
    }

    private static void ValidateContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ValidationException("Message content is required.");
        }

        if (content.Length > MaxMessageLength)
        {
            throw new ValidationException($"Message content cannot be longer than {MaxMessageLength} characters.");
        }
    }
}
