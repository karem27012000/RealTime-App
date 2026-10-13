using ChatBot.Application.DTOs;
using ChatBot.Application.Exceptions;
using ChatBot.Application.Services;
using ChatBot.Application.Tests.Fakes;

namespace ChatBot.Application.Tests.Services;

public class ConversationServiceTests
{
    private readonly InMemoryConversationRepository _repository = new();
    private readonly ConversationService _service;

    public ConversationServiceTests() => _service = new ConversationService(_repository);

    [Fact]
    public async Task CreateAsync_saves_and_returns_the_conversation()
    {
        var result = await _service.CreateAsync(new CreateConversationRequest("Homework help"));

        Assert.Equal("Homework help", result.Title);
        Assert.Single(_repository.Conversations);
        Assert.Equal(1, _repository.SaveChangesCalls);
    }

    [Fact]
    public async Task RenameAsync_changes_the_title()
    {
        var created = await _service.CreateAsync(new CreateConversationRequest(null));

        var renamed = await _service.RenameAsync(created.Id, new RenameConversationRequest("Renamed"));

        Assert.Equal("Renamed", renamed.Title);
    }

    [Fact]
    public async Task RenameAsync_with_empty_title_throws_ValidationException()
    {
        var created = await _service.CreateAsync(new CreateConversationRequest(null));

        await Assert.ThrowsAsync<ValidationException>(
            () => _service.RenameAsync(created.Id, new RenameConversationRequest(" ")));
    }

    [Fact]
    public async Task DeleteAsync_removes_the_conversation()
    {
        var created = await _service.CreateAsync(new CreateConversationRequest(null));

        await _service.DeleteAsync(created.Id);

        Assert.Empty(_repository.Conversations);
    }

    [Fact]
    public async Task GetByIdAsync_unknown_id_throws_NotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(Guid.NewGuid()));
    }
}
