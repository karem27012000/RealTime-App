using System.Runtime.CompilerServices;
using ChatBot.Application.Abstractions;

namespace ChatBot.Application.Tests.Fakes;

/// <summary>
/// Returns a fixed list of chunks and remembers the history it received.
/// </summary>
public sealed class FakeChatCompletionService(params string[] chunks) : IChatCompletionService
{
    public IReadOnlyList<ChatTurn>? LastHistory { get; private set; }

    public async IAsyncEnumerable<string> StreamReplyAsync(
        IReadOnlyList<ChatTurn> history,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        LastHistory = history;
        foreach (var chunk in chunks)
        {
            await Task.Yield();
            yield return chunk;
        }
    }
}
