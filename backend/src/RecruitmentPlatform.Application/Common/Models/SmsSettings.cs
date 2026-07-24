namespace RecruitmentPlatform.Application.Common.Models;

/// <summary>
/// Twilio configuration for SMS delivery. When <see cref="IsConfigured"/> is false the platform
/// falls back to the logging channel, so the app runs unchanged without an SMS provider.
/// </summary>
public class SmsSettings
{
    public const string SectionName = "Sms";

    /// <summary>Master switch. Set to false to force the logging channel.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Twilio Account SID (starts with "AC").</summary>
    public string? AccountSid { get; set; }

    /// <summary>Twilio auth token. Keep this out of appsettings.json — use secrets or env vars.</summary>
    public string? AuthToken { get; set; }

    /// <summary>Sending number in E.164 form, e.g. "+15551234567". A Messaging Service SID also works.</summary>
    public string? FromNumber { get; set; }

    /// <summary>Per-message wall-clock budget, so a hung provider cannot stall a request.</summary>
    public int TimeoutSeconds { get; set; } = 20;

    /// <summary>
    /// SMS is billed per segment and is far more intrusive than email, so bodies are truncated
    /// rather than silently fanning out into a multi-part message.
    /// </summary>
    public int MaxLength { get; set; } = 320;

    /// <summary>True when real Twilio credentials are available to send through.</summary>
    public bool IsConfigured => Enabled
        && !string.IsNullOrWhiteSpace(AccountSid)
        && !string.IsNullOrWhiteSpace(AuthToken)
        && !string.IsNullOrWhiteSpace(FromNumber);
}
