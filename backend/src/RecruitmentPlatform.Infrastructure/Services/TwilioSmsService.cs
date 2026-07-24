using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RecruitmentPlatform.Application.Common.Models;
using RecruitmentPlatform.Application.Interfaces.Infrastructure;

namespace RecruitmentPlatform.Infrastructure.Services;

/// <summary>
/// Sends SMS through Twilio's REST API over <see cref="HttpClient"/>. The wire format is a single
/// form-encoded POST, so the full Twilio SDK would add a sizeable dependency for no benefit and
/// would make swapping providers harder. When credentials are absent the call degrades to a log
/// line instead of throwing.
/// </summary>
public partial class TwilioSmsService : ISmsService
{
    private const string ApiRoot = "https://api.twilio.com/2010-04-01/Accounts";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SmsSettings _settings;
    private readonly ILogger<TwilioSmsService> _logger;

    public TwilioSmsService(IHttpClientFactory httpClientFactory, IOptions<SmsSettings> options, ILogger<TwilioSmsService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(string toPhoneNumber, string message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toPhoneNumber))
        {
            // Phone numbers are optional on User, so a missing one is normal, not an error.
            _logger.LogDebug("SMS skipped: recipient has no phone number.");
            return;
        }

        var normalized = Normalize(toPhoneNumber);
        if (normalized is null)
        {
            _logger.LogWarning("SMS skipped: '{Phone}' is not a usable E.164 number.", toPhoneNumber);
            return;
        }

        var body = Truncate(message, _settings.MaxLength);

        if (!_settings.IsConfigured)
        {
            _logger.LogInformation("SMS (not configured, logged only) -> {Phone} | {Message}", normalized, body);
            return;
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(_settings.TimeoutSeconds));

        var client = _httpClientFactory.CreateClient(nameof(TwilioSmsService));

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{ApiRoot}/{_settings.AccountSid}/Messages.json")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["To"] = normalized,
                ["From"] = _settings.FromNumber!,
                ["Body"] = body,
            }),
        };

        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{_settings.AccountSid}:{_settings.AuthToken}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        using var response = await client.SendAsync(request, timeoutCts.Token);

        if (!response.IsSuccessStatusCode)
        {
            // Surface Twilio's error payload — it carries the actionable code (e.g. 21610 unsubscribed).
            var payload = await response.Content.ReadAsStringAsync(timeoutCts.Token);
            _logger.LogError("SMS failed -> {Phone}: {Status} {Payload}", normalized, (int)response.StatusCode, payload);
            return;
        }

        _logger.LogInformation("SMS sent -> {Phone} ({Length} chars)", normalized, body.Length);
    }

    /// <summary>
    /// Coerces a stored number to E.164, which is the only format Twilio accepts. Numbers that are
    /// not already international are rejected rather than guessed at — inferring a country code
    /// would silently text the wrong person.
    /// </summary>
    private static string? Normalize(string phone)
    {
        var cleaned = NonDialRegex().Replace(phone.Trim(), string.Empty);

        if (!cleaned.StartsWith('+'))
        {
            return null;
        }

        // '+' plus 8–15 digits, per E.164.
        return cleaned.Length is >= 9 and <= 16 ? cleaned : null;
    }

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..(maxLength - 1)].TrimEnd() + "…";

    [GeneratedRegex(@"[^\d+]")]
    private static partial Regex NonDialRegex();
}
