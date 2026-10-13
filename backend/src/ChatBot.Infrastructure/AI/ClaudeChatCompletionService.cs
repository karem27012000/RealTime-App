using System.Runtime.CompilerServices;
using Anthropic;
using Anthropic.Models.Beta.Messages;
using ChatBot.Application.Abstractions;
using ChatBot.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChatBot.Infrastructure.AI;

/// <summary>
/// Sends the conversation to the Claude API and streams the answer back.
/// </summary>
public sealed class ClaudeChatCompletionService(
    AnthropicClient client,
    IOptions<ChatBotOptions> options,
    ILogger<ClaudeChatCompletionService> logger) : IChatCompletionService
{
    private readonly ClaudeOptions _options = options.Value.Claude;

    public async IAsyncEnumerable<string> StreamReplyAsync(
        IReadOnlyList<ChatTurn> history,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var parameters = new MessageCreateParams
        {
            Model = _options.Model,
            MaxTokens = _options.MaxTokens,
            System = _options.SystemPrompt,
            OutputConfig = new BetaOutputConfig { Effort = ParseEffort(_options.Effort) },
            Messages = history.Select(ToClaudeMessage).ToList(),

            // If Claude declines a request, let the API retry it on a suitable fallback model.
            Betas = ["server-side-fallback-2026-07-01"],
            Fallbacks = new Default(),
        };

        var stopReason = string.Empty;

        await foreach (var streamEvent in client.Beta.Messages.CreateStreaming(parameters, cancellationToken))
        {
            if (streamEvent.TryPickContentBlockDelta(out var blockDelta) &&
                blockDelta.Delta.TryPickText(out var text))
            {
                yield return text.Text;
            }
            else if (streamEvent.TryPickDelta(out var messageDelta) && messageDelta.Delta.StopReason is { } reason)
            {
                stopReason = reason.Raw();
            }
        }

        if (stopReason == "refusal")
        {
            logger.LogWarning("Claude declined to answer this request");
            yield return "Sorry, I can't help with that request.";
        }
        else if (stopReason == "max_tokens")
        {
            yield return "\n\n(The reply was cut off because it reached the maximum length.)";
        }
    }

    private static BetaMessageParam ToClaudeMessage(ChatTurn turn) => new()
    {
        Role = turn.Role == MessageRole.User ? Role.User : Role.Assistant,
        Content = turn.Content,
    };

    private static Effort ParseEffort(string effort) => effort.ToLowerInvariant() switch
    {
        "medium" => Effort.Medium,
        "high" => Effort.High,
        "xhigh" => Effort.Xhigh,
        "max" => Effort.Max,
        _ => Effort.Low,
    };
}
