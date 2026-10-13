using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using ChatBot.Application.Abstractions;

namespace ChatBot.Infrastructure.AI;

/// <summary>
/// Offline bot used when no API key is configured. It lets you run and test the
/// whole app (database, SignalR streaming, Angular UI) without calling any AI service.
/// </summary>
public sealed class DemoChatCompletionService : IChatCompletionService
{
    public async IAsyncEnumerable<string> StreamReplyAsync(
        IReadOnlyList<ChatTurn> history,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var question = history.LastOrDefault()?.Content ?? string.Empty;
        var reply = BuildReply(question, history.Count);

        // Send the reply word by word to simulate a real streaming model.
        foreach (var word in reply.Split(' '))
        {
            await Task.Delay(40, cancellationToken);
            yield return word + " ";
        }
    }

    private static string BuildReply(string question, int messageCount)
    {
        var text = question.ToLowerInvariant();

        if (Regex.IsMatch(text, @"\b(hello|hi|hey)\b"))
        {
            return "Hello! I am the demo bot. Configure a Claude API key to talk to a real AI model.";
        }

        if (Regex.IsMatch(text, @"\btime\b"))
        {
            return $"The server time is {DateTime.Now:HH:mm:ss}.";
        }

        return $"You said: \"{question}\". This conversation has {messageCount} message(s) so far. " +
               "I am running in demo mode, so I can only echo what you write.";
    }
}
