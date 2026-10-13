using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using ChatApp.Domain.Exceptions;

namespace ChatApp.Application.Tests.Domain;

public class ChatRoomTests
{
    private readonly Guid _alice = Guid.NewGuid();
    private readonly Guid _bob = Guid.NewGuid();

    [Fact]
    public void Group_creator_becomes_the_first_member()
    {
        var room = ChatRoom.CreateGroup("Trainees", _alice);

        Assert.Equal(RoomType.Group, room.Type);
        Assert.Equal("Trainees", room.Name);
        Assert.True(room.IsMember(_alice));
        Assert.Single(room.Members);
    }

    [Fact]
    public void Private_chat_has_both_users_as_members()
    {
        var room = ChatRoom.CreatePrivate(_alice, _bob);

        Assert.True(room.IsMember(_alice));
        Assert.True(room.IsMember(_bob));
        Assert.Equal(2, room.Members.Count);
    }

    [Fact]
    public void Private_key_is_the_same_whoever_starts_the_chat()
    {
        Assert.Equal(ChatRoom.BuildPrivateKey(_alice, _bob), ChatRoom.BuildPrivateKey(_bob, _alice));
    }

    [Fact]
    public void Cannot_start_private_chat_with_yourself()
    {
        Assert.Throws<DomainException>(() => ChatRoom.CreatePrivate(_alice, _alice));
    }

    [Fact]
    public void Joining_twice_does_not_duplicate_the_member()
    {
        var room = ChatRoom.CreateGroup("Trainees", _alice);

        room.Join(_bob);
        room.Join(_bob);

        Assert.Equal(2, room.Members.Count);
    }

    [Fact]
    public void Cannot_join_or_leave_a_private_chat()
    {
        var room = ChatRoom.CreatePrivate(_alice, _bob);

        Assert.Throws<DomainException>(() => room.Join(Guid.NewGuid()));
        Assert.Throws<DomainException>(() => room.Leave(_alice));
    }

    [Fact]
    public void Only_members_can_post_messages()
    {
        var room = ChatRoom.CreateGroup("Trainees", _alice);

        Assert.Throws<DomainException>(() => room.PostMessage(_bob, "Hi"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_messages_are_rejected(string content)
    {
        var room = ChatRoom.CreateGroup("Trainees", _alice);

        Assert.Throws<DomainException>(() => room.PostMessage(_alice, content));
    }

    [Fact]
    public void Posting_a_message_trims_it_and_updates_activity_time()
    {
        var room = ChatRoom.CreateGroup("Trainees", _alice);
        var before = room.UpdatedAt;

        var message = room.PostMessage(_alice, "  Hello  ");

        Assert.Equal("Hello", message.Content);
        Assert.Equal(room.Id, message.RoomId);
        Assert.True(room.UpdatedAt >= before);
    }
}
