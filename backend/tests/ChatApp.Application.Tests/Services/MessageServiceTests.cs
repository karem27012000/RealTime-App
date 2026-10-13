using ChatApp.Application.Exceptions;
using ChatApp.Application.Services;
using ChatApp.Application.Tests.Fakes;
using ChatApp.Domain.Entities;

namespace ChatApp.Application.Tests.Services;

public class MessageServiceTests
{
    private readonly FakeUserRepository _users = new();
    private readonly FakeChatRoomRepository _rooms;
    private readonly RecordingChatNotifier _notifier = new();
    private readonly MessageService _service;

    public MessageServiceTests()
    {
        _rooms = new FakeChatRoomRepository(_users);
        _service = new MessageService(_rooms, _users, _notifier);
    }

    [Fact]
    public async Task Sent_message_is_saved_and_pushed_to_every_member()
    {
        var alice = _users.Add("alice");
        var bob = _users.Add("bob");
        var room = ChatRoom.CreatePrivate(alice.Id, bob.Id);
        _rooms.Rooms.Add(room);

        var message = await _service.SendAsync(room.Id, alice.Id, "Hi Bob!");

        Assert.Equal("alice", message.SenderNickname);
        Assert.Single(_rooms.Messages);
        var (pushed, recipients) = Assert.Single(_notifier.Messages);
        Assert.Equal(message.Id, pushed.Id);
        Assert.Equivalent(new[] { alice.Id, bob.Id }, recipients);
    }

    [Fact]
    public async Task Non_members_cannot_send_messages()
    {
        var alice = _users.Add("alice");
        var eve = _users.Add("eve");
        var room = ChatRoom.CreateGroup("Trainees", alice.Id);
        _rooms.Rooms.Add(room);

        await Assert.ThrowsAsync<ForbiddenException>(() => _service.SendAsync(room.Id, eve.Id, "Hello"));
        Assert.Empty(_notifier.Messages);
    }

    [Fact]
    public async Task Messages_are_returned_oldest_first()
    {
        var alice = _users.Add("alice");
        var room = ChatRoom.CreateGroup("Trainees", alice.Id);
        _rooms.Rooms.Add(room);

        await _service.SendAsync(room.Id, alice.Id, "first");
        await Task.Delay(5);
        await _service.SendAsync(room.Id, alice.Id, "second");

        var messages = await _service.GetMessagesAsync(room.Id, alice.Id, before: null, take: 50);

        Assert.Equal(["first", "second"], messages.Select(m => m.Content));
    }
}
