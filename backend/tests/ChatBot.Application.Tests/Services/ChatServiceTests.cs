using ChatBot.Application.Exceptions;
using ChatBot.Application.Services;
using ChatBot.Application.Tests.Fakes;
using ChatBot.Domain.Entities;
using ChatBot.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace ChatBot.Application.Tests.Services;

public class ChatServiceTests
{
    private readonly InMemoryConversationRepository _repository = new();

    private ChatService CreateService(FakeChatCompletionService bot) =>
        new(_repository, bot, NullLogger<ChatService>.Instance);

    [Fact]
    public async Task StreamReplyAsync_streams_chunks_and_saves_both_messages()
    {
        var conversation = new Conversation(null);
        _repository.Conversations.Add(conversation);
        var service = CreateService(new FakeChatCompletionService("Hello", " there", "!"));

        var chunks = new List<string>();
        await foreach (var chunk in service.StreamReplyAsync(conversation.Id, "Hi bot"))
        {
            chunks.Add(chunk);
        }

        Assert.Equal(["Hello", " there", "!"], chunks);
        Assert.Collection(
            conversation.Messages,
            m => { Assert.Equal(MessageRole.User, m.Role); Assert.Equal("Hi bot", m.Content); },
            m => { Assert.Equal(MessageRole.Assistant, m.Role); Assert.Equal("Hello there!", m.Content); });
    }

    [Fact]
    public async Task StreamReplyAsync_sends_full_history_to_the_bot()
    {
        var conversation = new Conversation(null);
        conversation.AddMessage(MessageRole.User, "First question");
        conversation.AddMessage(MessageRole.Assistant, "First answer");
        _repository.Conversations.Add(conversation);
        var bot = new FakeChatCompletionService("ok");

        await foreach (var _ in CreateService(bot).StreamReplyAsync(conversation.Id, "Second question")) { }

        Assert.NotNull(bot.LastHistory);
        Assert.Equal(3, bot.LastHistory.Count);
        Assert.Equal("Second question", bot.LastHistory[^1].Content);
    }

    [Fact]
    public async Task SendMessageAsync_returns_the_assistant_message()
    {
        var conversation = new Conversation(null);
        _repository.Conversations.Add(conversation);

        var reply = await CreateService(new FakeChatCompletionService("Hi ", "there")).SendMessageAsync(conversation.Id, "Hello");

        Assert.Equal("assistant", reply.Role);
        Assert.Equal("Hi there", reply.Content);
    }

    [Fact]
    public async Task Unknown_conversation_throws_NotFoundException()
    {
        var service = CreateService(new FakeChatCompletionService("x"));

        await Assert.ThrowsAsync<NotFoundException>(() => service.SendMessageAsync(Guid.NewGuid(), "Hello"));
    }

    [Fact]
    public async Task Empty_message_throws_ValidationException()
    {
        var conversation = new Conversation(null);
        _repository.Conversations.Add(conversation);
        var service = CreateService(new FakeChatCompletionService("x"));

        await Assert.ThrowsAsync<ValidationException>(() => service.SendMessageAsync(conversation.Id, "  "));
    }

    [Fact]
    public async Task Too_long_message_throws_ValidationException()
    {
        var conversation = new Conversation(null);
        _repository.Conversations.Add(conversation);
        var service = CreateService(new FakeChatCompletionService("x"));
        var content = new string('a', ChatService.MaxMessageLength + 1);

        await Assert.ThrowsAsync<ValidationException>(() => service.SendMessageAsync(conversation.Id, content));
    }
}
