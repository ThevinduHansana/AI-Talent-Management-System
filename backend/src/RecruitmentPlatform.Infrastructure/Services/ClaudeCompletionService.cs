using System.Text;
using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RecruitmentPlatform.Application.Common.Models;
using RecruitmentPlatform.Application.Interfaces.Infrastructure;

namespace RecruitmentPlatform.Infrastructure.Services;

/// <summary>
/// <see cref="IAiCompletionService"/> backed by the Claude API (official Anthropic SDK).
///
/// Requests use adaptive thinking plus structured outputs, so replies are schema-valid JSON the
/// application layer can deserialize directly. Provider failures are swallowed and logged: the
/// caller sees <c>null</c> and falls back to its deterministic heuristic rather than failing the
/// user's request.
/// </summary>
public class ClaudeCompletionService : IAiCompletionService
{
    private readonly AiSettings _settings;
    private readonly ILogger<ClaudeCompletionService> _logger;
    private readonly AnthropicClient? _client;

    public ClaudeCompletionService(IOptions<AiSettings> settings, ILogger<ClaudeCompletionService> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        if (!_settings.Enabled)
        {
            _logger.LogInformation("AI features are disabled by configuration; using deterministic heuristics.");
            return;
        }

        // An explicit Ai:ApiKey wins; otherwise the SDK's own ANTHROPIC_API_KEY convention applies.
        var apiKey = string.IsNullOrWhiteSpace(_settings.ApiKey)
            ? Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")
            : _settings.ApiKey;

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning(
                "No Anthropic API key configured (Ai:ApiKey or ANTHROPIC_API_KEY); AI features will use deterministic heuristics.");
            return;
        }

        _client = new AnthropicClient { ApiKey = apiKey };
        _logger.LogInformation("Claude AI services enabled using model {Model} at effort {Effort}.", _settings.Model, _settings.Effort);
    }

    public bool IsEnabled => _client is not null;

    public async Task<string?> CompleteJsonAsync(AiJsonRequest request, CancellationToken cancellationToken = default)
    {
        if (_client is null) return null;

        var schema = ParseSchema(request.JsonSchema);
        if (schema is null) return null;

        // Bound the call so a slow provider can't hold a user request open indefinitely.
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(_settings.TimeoutSeconds));

        try
        {
            var parameters = new MessageCreateParams
            {
                Model = _settings.Model,
                MaxTokens = request.MaxTokens ?? _settings.MaxTokens,
                System = new List<TextBlockParam> { new() { Text = request.SystemPrompt } },
                Thinking = new ThinkingConfigAdaptive(),
                OutputConfig = new OutputConfig
                {
                    Effort = ParseEffort(_settings.Effort),
                    Format = new JsonOutputFormat { Schema = schema },
                },
                Messages = [new() { Role = Role.User, Content = request.UserPrompt }],
            };

            var message = await _client.Messages.Create(parameters, cancellationToken: timeout.Token);

            if (message.StopReason == StopReason.Refusal)
            {
                _logger.LogWarning("Claude declined the request; falling back to heuristics.");
                return null;
            }

            if (message.StopReason == StopReason.MaxTokens)
            {
                // The JSON is truncated and would not parse — treat it as a failed call.
                _logger.LogWarning("Claude response hit the {MaxTokens}-token output cap; falling back to heuristics.",
                    request.MaxTokens ?? _settings.MaxTokens);
                return null;
            }

            var text = new StringBuilder();
            foreach (var block in message.Content)
            {
                if (block.TryPickText(out TextBlock? textBlock))
                {
                    text.Append(textBlock.Text);
                }
            }

            var payload = text.ToString();
            return string.IsNullOrWhiteSpace(payload) ? null : payload;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Claude request timed out after {Seconds}s; falling back to heuristics.", _settings.TimeoutSeconds);
            return null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Claude request failed; falling back to heuristics.");
            return null;
        }
    }

    private Dictionary<string, JsonElement>? ParseSchema(string schemaJson)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(schemaJson);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON schema supplied to the AI completion service.");
            return null;
        }
    }

    private static Effort ParseEffort(string? effort) => effort?.Trim().ToLowerInvariant() switch
    {
        "low" => Effort.Low,
        "medium" => Effort.Medium,
        "max" => Effort.Max,
        _ => Effort.High,
    };
}
