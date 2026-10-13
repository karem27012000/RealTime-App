namespace ChatBot.Infrastructure.AI;

/// <summary>
/// Bound from the "ChatBot" section of appsettings.json.
/// </summary>
public sealed class ChatBotOptions
{
    public const string SectionName = "ChatBot";

    /// <summary>
    /// "Claude" to use the Claude API, or "Demo" for an offline bot that needs no API key.
    /// </summary>
    public string Provider { get; set; } = "Demo";

    public ClaudeOptions Claude { get; set; } = new();
}

public sealed class ClaudeOptions
{
    /// <summary>
    /// Leave empty in appsettings.json and set it with user-secrets or the ANTHROPIC_API_KEY environment variable.
    /// </summary>
    public string? ApiKey { get; set; }

    public string Model { get; set; } = "claude-opus-5-5";

    public int MaxTokens { get; set; } = 16000;

    /// <summary>
    /// How much effort the model spends thinking: low, medium, high, xhigh or max.
    /// "low" keeps a chat bot fast and cheap.
    /// </summary>
    public string Effort { get; set; } = "low";

    public string SystemPrompt { get; set; } =
        "You are a friendly and helpful assistant inside a chat app. Keep answers clear and concise.";
}
