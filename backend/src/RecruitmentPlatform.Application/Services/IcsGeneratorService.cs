using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using RecruitmentPlatform.Application.Interfaces.Services;

namespace RecruitmentPlatform.Application.Services;

/// <summary>
/// Generates RFC 5545 (iCalendar) documents for interview invitations.
/// <para>
/// Two details of the spec are easy to get wrong and both break real clients: content lines must
/// be folded at 75 octets, and TEXT values must escape backslash, semicolon, comma and newline.
/// Outlook in particular rejects an unfolded long DESCRIPTION outright.
/// </para>
/// </summary>
public class IcsGeneratorService : IIcsGeneratorService
{
    private const string ProductId = "-//GetCareers//Recruitment Platform//EN";
    private const int MaxLineOctets = 75;

    private readonly ILogger<IcsGeneratorService> _logger;

    public IcsGeneratorService(ILogger<IcsGeneratorService> logger) => _logger = logger;

    public byte[] Generate(CalendarInvite invite)
    {
        var lines = new List<string>
        {
            "BEGIN:VCALENDAR",
            "VERSION:2.0",
            $"PRODID:{ProductId}",
            "CALSCALE:GREGORIAN",
            // REQUEST asks the client to add/update the event; CANCEL withdraws it.
            $"METHOD:{(invite.IsCancelled ? "CANCEL" : "REQUEST")}",
            "BEGIN:VEVENT",
            $"UID:{invite.Uid}",
            $"SEQUENCE:{invite.Sequence}",
            $"DTSTAMP:{FormatUtc(DateTime.UtcNow)}",
            $"DTSTART:{FormatUtc(invite.StartUtc)}",
            $"DTEND:{FormatUtc(invite.EndUtc)}",
            $"SUMMARY:{Escape(invite.Title)}",
            $"DESCRIPTION:{Escape(invite.Description)}",
        };

        if (!string.IsNullOrWhiteSpace(invite.Location))
        {
            lines.Add($"LOCATION:{Escape(invite.Location)}");
        }

        if (!string.IsNullOrWhiteSpace(invite.Url))
        {
            lines.Add($"URL:{invite.Url}");
        }

        lines.Add($"ORGANIZER;CN={Escape(invite.OrganizerName)}:mailto:{invite.OrganizerEmail}");
        lines.Add(
            $"ATTENDEE;CN={Escape(invite.AttendeeName)};ROLE=REQ-PARTICIPANT;" +
            $"PARTSTAT=NEEDS-ACTION;RSVP=TRUE:mailto:{invite.AttendeeEmail}");

        lines.Add($"STATUS:{(invite.IsCancelled ? "CANCELLED" : "CONFIRMED")}");
        lines.Add("TRANSP:OPAQUE");

        // A client-side alarm 15 minutes before. Google/Outlook apply their own defaults too;
        // this guarantees at least one reminder for clients that have none.
        if (!invite.IsCancelled)
        {
            lines.Add("BEGIN:VALARM");
            lines.Add("TRIGGER:-PT15M");
            lines.Add("ACTION:DISPLAY");
            lines.Add($"DESCRIPTION:{Escape(invite.Title)}");
            lines.Add("END:VALARM");
        }

        lines.Add("END:VEVENT");
        lines.Add("END:VCALENDAR");

        var sb = new StringBuilder();
        foreach (var line in lines)
        {
            // RFC 5545 mandates CRLF line endings.
            sb.Append(Fold(line)).Append("\r\n");
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        _logger.LogInformation("ICS file generated for event {Uid} ({Bytes} bytes).", invite.Uid, bytes.Length);
        return bytes;
    }

    /// <summary>UTC timestamp in iCalendar basic format: yyyyMMddTHHmmssZ.</summary>
    private static string FormatUtc(DateTime value)
    {
        var utc = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };

        return utc.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Escapes a TEXT value per RFC 5545 §3.3.11. Backslash must be escaped first, otherwise the
    /// escapes introduced for the other characters would themselves be re-escaped.
    /// </summary>
    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value
            .Replace("\\", "\\\\")
            .Replace(";", "\\;")
            .Replace(",", "\\,")
            .Replace("\r\n", "\\n")
            .Replace("\n", "\\n")
            .Replace("\r", "\\n");
    }

    /// <summary>
    /// Folds a content line to 75 octets per RFC 5545 §3.1, continuing with CRLF + a single space.
    /// The limit is octets, not characters, so multi-byte UTF-8 is measured by encoded length and
    /// never split mid-character.
    /// </summary>
    private static string Fold(string line)
    {
        if (Encoding.UTF8.GetByteCount(line) <= MaxLineOctets)
        {
            return line;
        }

        var sb = new StringBuilder();
        var octets = 0;
        var isContinuation = false;

        foreach (var rune in line.EnumerateRunes())
        {
            var runeOctets = Encoding.UTF8.GetByteCount(rune.ToString());

            // Continuation lines carry a leading space that counts toward the octet budget.
            if (octets + runeOctets > (isContinuation ? MaxLineOctets - 1 : MaxLineOctets))
            {
                sb.Append("\r\n ");
                octets = 0;
                isContinuation = true;
            }

            sb.Append(rune);
            octets += runeOctets;
        }

        return sb.ToString();
    }
}
