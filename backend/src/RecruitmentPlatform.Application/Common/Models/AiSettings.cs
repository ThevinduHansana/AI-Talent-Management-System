namespace RecruitmentPlatform.Application.Common.Models;

/// <summary>
/// Configuration for the Claude-backed AI services. When no API key is resolvable the AI
/// implementations stay dormant and the deterministic heuristics are used instead, so the
/// platform runs unchanged without a key.
/// </summary>
public class AiSettings
{
    public const string SectionName = "Ai";

    /// <summary>Master switch. Set to false to force the deterministic heuristics.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Anthropic API key. Leave empty to fall back to the <c>ANTHROPIC_API_KEY</c> environment
    /// variable, which the SDK reads on its own.
    /// </summary>
    public string? ApiKey { get; set; }

    public string Model { get; set; } = "claude-opus-4-8";

    /// <summary>Thinking/output effort: low | medium | high | xhigh | max.</summary>
    public string Effort { get; set; } = "high";

    public int MaxTokens { get; set; } = 16000;

    /// <summary>Per-request wall-clock budget; on expiry the caller falls back to the heuristic.</summary>
    public int TimeoutSeconds { get; set; } = 120;

    /// <summary>Resume text is truncated to this many characters before being sent to the model.</summary>
    public int MaxResumeCharacters { get; set; } = 12000;
}
