namespace RecruitmentPlatform.Application.Interfaces.Infrastructure;

/// <summary>
/// A single structured-output completion request. <paramref name="JsonSchema"/> is a JSON Schema
/// document (as raw JSON) the model's response is constrained to.
/// </summary>
/// <param name="SystemPrompt">Stable instructions; cached by the provider across requests.</param>
/// <param name="UserPrompt">The per-request payload (resume text, job/candidate profile, …).</param>
/// <param name="JsonSchema">JSON Schema the reply must satisfy.</param>
/// <param name="MaxTokens">Overrides the configured output budget when set.</param>
public record AiJsonRequest(string SystemPrompt, string UserPrompt, string JsonSchema, int? MaxTokens = null);

/// <summary>
/// Abstraction over a large-language-model provider, exposing exactly what the application layer
/// needs: a prompt in, schema-validated JSON out. Implemented by the Claude/Anthropic client in
/// the infrastructure layer and swappable without touching the AI services.
/// </summary>
public interface IAiCompletionService
{
    /// <summary>False when no credentials are configured — callers use their heuristic fallback.</summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Returns the model's JSON reply, or <c>null</c> when the provider is disabled or the call
    /// failed. Implementations never throw for provider-side problems: callers degrade gracefully.
    /// </summary>
    Task<string?> CompleteJsonAsync(AiJsonRequest request, CancellationToken cancellationToken = default);
}
