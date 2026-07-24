using System.Text;
using Microsoft.Extensions.Logging;
using RecruitmentPlatform.Application.Interfaces.Services;

namespace RecruitmentPlatform.Application.Services;

/// <summary>
/// Builds Google and Outlook "add to calendar" deep links.
/// <para>
/// These are ordinary URLs the recipient clicks, so there is no OAuth consent, no stored refresh
/// token and no Graph/Calendar API quota to manage. The trade-off is that we cannot read back
/// whether the event was actually added — acceptable, because the .ics attachment covers clients
/// that aren't Google or Outlook.
/// </para>
/// </summary>
public class CalendarLinkService : ICalendarLinkService
{
    private const string GoogleBase = "https://calendar.google.com/calendar/render";
    private const string OutlookBase = "https://outlook.live.com/calendar/0/deeplink/compose";

    private readonly ILogger<CalendarLinkService> _logger;

    public CalendarLinkService(ILogger<CalendarLinkService> logger) => _logger = logger;

    public string BuildGoogleCalendarUrl(CalendarInvite invite)
    {
        // Google expects a compact UTC basic-format range: 20260801T140000Z/20260801T150000Z
        var dates = $"{FormatGoogle(invite.StartUtc)}/{FormatGoogle(invite.EndUtc)}";

        var url = new StringBuilder(GoogleBase)
            .Append("?action=TEMPLATE")
            .Append("&text=").Append(Uri.EscapeDataString(invite.Title))
            .Append("&dates=").Append(dates)
            .Append("&details=").Append(Uri.EscapeDataString(BuildDetails(invite)))
            .Append("&location=").Append(Uri.EscapeDataString(invite.Location ?? string.Empty))
            .ToString();

        _logger.LogInformation("Calendar link generated (Google) for event {Uid}.", invite.Uid);
        return url;
    }

    public string BuildOutlookCalendarUrl(CalendarInvite invite)
    {
        // Outlook expects ISO-8601 UTC ("2026-08-01T14:00:00Z"), not the compact Google form.
        var url = new StringBuilder(OutlookBase)
            .Append("?path=").Append(Uri.EscapeDataString("/calendar/action/compose"))
            .Append("&rru=addevent")
            .Append("&subject=").Append(Uri.EscapeDataString(invite.Title))
            .Append("&startdt=").Append(Uri.EscapeDataString(FormatIso(invite.StartUtc)))
            .Append("&enddt=").Append(Uri.EscapeDataString(FormatIso(invite.EndUtc)))
            .Append("&body=").Append(Uri.EscapeDataString(BuildDetails(invite)))
            .Append("&location=").Append(Uri.EscapeDataString(invite.Location ?? string.Empty))
            .ToString();

        _logger.LogInformation("Calendar link generated (Outlook) for event {Uid}.", invite.Uid);
        return url;
    }

    /// <summary>
    /// Body text shared by both providers. The meeting link is repeated here because neither
    /// provider renders the ICS URL property in the quick-add flow.
    /// </summary>
    private static string BuildDetails(CalendarInvite invite)
    {
        var sb = new StringBuilder(invite.Description);

        if (!string.IsNullOrWhiteSpace(invite.Url))
        {
            sb.Append("\n\nJoin: ").Append(invite.Url);
        }

        return sb.ToString();
    }

    /// <summary>Compact UTC form required by Google: yyyyMMddTHHmmssZ.</summary>
    private static string FormatGoogle(DateTime utc)
        => EnsureUtc(utc).ToString("yyyyMMdd'T'HHmmss'Z'", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>Extended ISO-8601 UTC form required by Outlook.</summary>
    private static string FormatIso(DateTime utc)
        => EnsureUtc(utc).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>
    /// Guards against an Unspecified-kind DateTime being emitted as if it were UTC. Values read
    /// back from Npgsql are UTC, but a value straight off a DTO may not be tagged.
    /// </summary>
    private static DateTime EnsureUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };
}
