using ChatApp.Application.DTOs;
using ChatApp.Application.Services;
using ChatApp.Application.Tests.Fakes;

namespace ChatApp.Application.Tests.Services;

public class UserServiceTests
{
    private readonly FakeUserRepository _users = new();
    private readonly FakePresenceTracker _presence = new();
    private readonly RecordingChatNotifier _notifier = new();
    private readonly UserService _service;

    public UserServiceTests() => _service = new UserService(_users, _presence, _notifier);

    [Fact]
    public async Task Login_creates_a_new_user_the_first_time()
    {
        var user = await _service.LoginAsync(new LoginRequest("karem"));

        Assert.Equal("karem", user.Nickname);
        Assert.Single(_users.Users);
    }

    [Fact]
    public async Task Login_with_the_same_nickname_returns_the_same_user()
    {
        var first = await _service.LoginAsync(new LoginRequest("karem"));
        var second = await _service.LoginAsync(new LoginRequest("KAREM"));

        Assert.Equal(first.Id, second.Id);
        Assert.Single(_users.Users);
    }

    [Fact]
    public async Task Presence_is_announced_only_on_first_connection_and_last_disconnection()
    {
        var user = _users.Add("karem");

        await _service.ConnectAsync(user.Id, "tab-1");
        await _service.ConnectAsync(user.Id, "tab-2");
        await _service.DisconnectAsync(user.Id, "tab-1");
        await _service.DisconnectAsync(user.Id, "tab-2");

        Assert.Collection(
            _notifier.PresenceChanges,
            online => Assert.True(online.IsOnline),
            offline => Assert.False(offline.IsOnline));
    }
}
