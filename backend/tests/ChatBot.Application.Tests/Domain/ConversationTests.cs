using ChatBot.Domain.Entities;
using ChatBot.Domain.Enums;

namespace ChatBot.Application.Tests.Domain;

public class ConversationTests
{
    [Fact]
    public void New_conversation_without_title_uses_default_title()
    {
        var conversation = new Conversation(null);

        Assert.Equal(Conversation.DefaultTitle, conversation.Title);
    }

    [Fact]
    public void First_user_message_becomes_the_title()
    {
        var conversation = new Conversation(null);

        conversation.AddMessage(MessageRole.User, "How do I learn Angular?");

        Assert.Equal("How do I learn Angular?", conversation.Title);
    }

    [Fact]
    public void Long_titles_are_shortened()
    {
        var conversation = new Conversation(new string('a', 300));

        Assert.Equal(Conversation.TitleMaxLength, conversation.Title.Length);
        Assert.EndsWith("...", conversation.Title);
    }

    [Fact]
    public void Custom_title_is_not_replaced_by_first_message()
    {
        var conversation = new Conversation("My chat");

        conversation.AddMessage(MessageRole.User, "Hello");

        Assert.Equal("My chat", conversation.Title);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_message_is_rejected(string content)
    {
        var conversation = new Conversation(null);

        Assert.Throws<ArgumentException>(() => conversation.AddMessage(MessageRole.User, content));
    }
}
