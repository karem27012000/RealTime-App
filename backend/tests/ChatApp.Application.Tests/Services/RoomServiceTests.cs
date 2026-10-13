using ChatApp.Application.DTOs;
using ChatApp.Application.Exceptions;
using ChatApp.Application.Services;
using ChatApp.Application.Tests.Fakes;

namespace ChatApp.Application.Tests.Services;

public class RoomServiceTests
{
    private readonly FakeUserRepository _users = new();
    private readonly FakeChatRoomRepository _rooms;
    private readonly RecordingChatNotifier _notifier = new();
    private readonly RoomService _service;

    public RoomServiceTests()
    {
        _rooms = new FakeChatRoomRepository(_users);
        _service = new RoomService(_rooms, _users, new FakePresenceTracker(), _notifier);
    }

    [Fact]
    public async Task Private_chat_is_named_after_the_other_user_and_both_are_notified()
    {
        var alice = _users.Add("alice");
        var bob = _users.Add("bob");

        var room = await _service.StartPrivateChatAsync(alice.Id, new StartPrivateChatRequest(bob.Id));

        Assert.Equal("bob", room.Name);
        Assert.Equal("private", room.Type);
        Assert.Equal(bob.Id, room.OtherUserId);
        Assert.Contains(_notifier.RoomsAdded, n => n.UserId == bob.Id && n.Room.Name == "alice");
    }

    [Fact]
    public async Task Starting_a_private_chat_twice_reuses_the_same_room()
    {
        var alice = _users.Add("alice");
        var bob = _users.Add("bob");

        var first = await _service.StartPrivateChatAsync(alice.Id, new StartPrivateChatRequest(bob.Id));
        var second = await _service.StartPrivateChatAsync(bob.Id, new StartPrivateChatRequest(alice.Id));

        Assert.Equal(first.Id, second.Id);
        Assert.Single(_rooms.Rooms);
    }

    [Fact]
    public async Task Created_group_is_listed_for_the_creator_and_joinable_by_others()
    {
        var alice = _users.Add("alice");
        var bob = _users.Add("bob");

        await _service.CreateGroupAsync(alice.Id, new CreateGroupRequest("Trainees"));

        Assert.Single(await _service.GetMyRoomsAsync(alice.Id));
        Assert.Single(await _service.GetJoinableGroupsAsync(bob.Id));
        Assert.Empty(await _service.GetJoinableGroupsAsync(alice.Id));
    }

    [Fact]
    public async Task Join_and_leave_a_group()
    {
        var alice = _users.Add("alice");
        var bob = _users.Add("bob");
        var group = await _service.CreateGroupAsync(alice.Id, new CreateGroupRequest("Trainees"));

        var joined = await _service.JoinGroupAsync(group.Id, bob.Id);
        Assert.Equal(2, joined.MemberCount);

        await _service.LeaveGroupAsync(group.Id, bob.Id);
        Assert.Empty(await _service.GetMyRoomsAsync(bob.Id));
    }

    [Fact]
    public async Task Non_members_cannot_see_room_details()
    {
        var alice = _users.Add("alice");
        var bob = _users.Add("bob");
        var group = await _service.CreateGroupAsync(alice.Id, new CreateGroupRequest("Trainees"));

        await Assert.ThrowsAsync<ForbiddenException>(() => _service.GetDetailsAsync(group.Id, bob.Id));
    }

    [Fact]
    public async Task Unknown_user_cannot_create_a_group()
    {
        await Assert.ThrowsAsync<UnauthorizedException>(
            () => _service.CreateGroupAsync(Guid.NewGuid(), new CreateGroupRequest("Trainees")));
    }
}
