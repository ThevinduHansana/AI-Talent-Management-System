using System.Globalization;
using System.Net;
using System.Text;

namespace RecruitmentPlatform.Application.Common.Email;

/// <summary>
/// Builds the HTML for outbound transactional email. Styling is inline because mail clients
/// (Gmail, Outlook) strip &lt;style&gt; blocks, and the layout is a single centred table for the
/// same reason — flexbox and grid are unreliable across clients.
/// </summary>
public static class EmailTemplates
{
    private const string Brand = "GetCareers";
    private const string Accent = "#4f46e5";
    private const string Ink = "#111827";
    private const string Muted = "#6b7280";
    private const string Border = "#e5e7eb";

    /// <summary>
    /// Wraps body content in the branded shell. <paramref name="heading"/> and
    /// <paramref name="bodyText"/> are HTML-encoded here, so callers pass plain text and cannot
    /// accidentally inject markup from user-supplied job titles or names.
    /// </summary>
    public static string Build(string heading, string bodyText, string? ctaLabel = null, string? ctaUrl = null, string? footnote = null)
    {
        var sb = new StringBuilder();

        sb.Append("<div style=\"margin:0;padding:24px 12px;background:#f3f4f6;")
          .Append("font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;\">");

        sb.Append("<table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" ")
          .Append("style=\"max-width:560px;margin:0 auto;background:#ffffff;border:1px solid ").Append(Border)
          .Append(";border-radius:12px;overflow:hidden;\"><tr><td style=\"padding:28px 32px;\">");

        // Wordmark
        sb.Append("<div style=\"font-size:18px;font-weight:700;color:").Append(Accent)
          .Append(";letter-spacing:-0.02em;margin-bottom:20px;\">").Append(Brand).Append("</div>");

        sb.Append("<h1 style=\"margin:0 0 12px;font-size:20px;line-height:1.3;font-weight:700;color:")
          .Append(Ink).Append(";\">").Append(Encode(heading)).Append("</h1>");

        sb.Append("<p style=\"margin:0 0 20px;font-size:15px;line-height:1.6;color:#374151;\">")
          .Append(Encode(bodyText)).Append("</p>");

        if (!string.IsNullOrWhiteSpace(ctaLabel) && !string.IsNullOrWhiteSpace(ctaUrl))
        {
            sb.Append("<table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"margin:0 0 20px;\"><tr><td ")
              .Append("style=\"background:").Append(Accent).Append(";border-radius:8px;\">")
              .Append("<a href=\"").Append(Encode(ctaUrl)).Append("\" ")
              .Append("style=\"display:inline-block;padding:11px 22px;font-size:15px;font-weight:600;")
              .Append("color:#ffffff;text-decoration:none;\">").Append(Encode(ctaLabel))
              .Append("</a></td></tr></table>");

            // Some clients suppress buttons; always offer the raw URL as a fallback.
            sb.Append("<p style=\"margin:0 0 20px;font-size:12px;line-height:1.5;color:").Append(Muted)
              .Append(";word-break:break-all;\">If the button does not work, paste this into your browser:<br>")
              .Append(Encode(ctaUrl)).Append("</p>");
        }

        if (!string.IsNullOrWhiteSpace(footnote))
        {
            sb.Append("<p style=\"margin:0;font-size:13px;line-height:1.5;color:").Append(Muted)
              .Append(";\">").Append(Encode(footnote)).Append("</p>");
        }

        sb.Append("</td></tr><tr><td style=\"padding:16px 32px;background:#fafafa;border-top:1px solid ")
          .Append(Border).Append(";font-size:12px;color:").Append(Muted)
          .Append(";\">You are receiving this because of activity on your ").Append(Brand)
          .Append(" account.</td></tr></table></div>");

        return sb.ToString();
    }

    /// <summary>Password-reset email. The token is embedded in the link, never shown as text.</summary>
    public static string PasswordReset(string firstName, string resetUrl, int expiryMinutes)
        => Build(
            $"Reset your password",
            $"Hi {firstName}, we received a request to reset your {Brand} password. Click the button below to choose a new one.",
            "Reset password",
            resetUrl,
            $"This link expires in {expiryMinutes} minutes. If you didn't request a reset, you can safely ignore this email — your password will not change.");

    /// <summary>Welcome email sent once, when a candidate self-registers.</summary>
    public static string Welcome(string firstName, string appUrl)
        => Build(
            $"Welcome to {Brand}, {firstName}",
            "Your account is ready. Complete your profile and upload a resume so recruiters can find you — "
            + "we'll email you when your application status changes and remind you before every interview.",
            "Complete your profile",
            appUrl,
            "If you didn't create this account, please ignore this email.");

    /// <summary>Details rendered into an interview invitation email.</summary>
    public record InterviewInvitationModel(
        string CandidateName,
        string CompanyName,
        string JobTitle,
        DateTime StartUtc,
        int DurationMinutes,
        string? Location,
        string? MeetingLink,
        string? Notes,
        string GoogleCalendarUrl,
        string OutlookCalendarUrl,
        bool IsReschedule);

    /// <summary>
    /// Interview invitation with "Add to Google Calendar" / "Add to Outlook Calendar" buttons and
    /// a note about the attached .ics file. Laid out as tables with inline CSS — Gmail and Outlook
    /// strip &lt;style&gt; blocks and ignore flexbox, so anything else degrades badly.
    /// </summary>
    public static string InterviewInvitation(InterviewInvitationModel model)
    {
        var sb = new StringBuilder();

        sb.Append("<div style=\"margin:0;padding:24px 12px;background:#f3f4f6;")
          .Append("font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;\">");

        sb.Append("<table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" ")
          .Append("style=\"max-width:600px;margin:0 auto;background:#ffffff;border:1px solid ").Append(Border)
          .Append(";border-radius:12px;overflow:hidden;\"><tr><td style=\"padding:28px 32px;\">");

        sb.Append("<div style=\"font-size:18px;font-weight:700;color:").Append(Accent)
          .Append(";letter-spacing:-0.02em;margin-bottom:20px;\">").Append(Brand).Append("</div>");

        var heading = model.IsReschedule ? "Your interview has been rescheduled" : "You have an interview invitation";
        sb.Append("<h1 style=\"margin:0 0 12px;font-size:20px;line-height:1.3;font-weight:700;color:")
          .Append(Ink).Append(";\">").Append(Encode(heading)).Append("</h1>");

        sb.Append("<p style=\"margin:0 0 20px;font-size:15px;line-height:1.6;color:#374151;\">Hi ")
          .Append(Encode(model.CandidateName)).Append(", ")
          .Append(model.IsReschedule
              ? "the details of your interview have changed. The updated details are below —"
              : "you've been invited to interview for a role at ")
          .Append(model.IsReschedule ? " please update your calendar." : Encode(model.CompanyName) + ".")
          .Append("</p>");

        // Detail table.
        sb.Append("<table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" ")
          .Append("style=\"width:100%;margin:0 0 24px;border-collapse:collapse;\">");

        AppendRow(sb, "Position", model.JobTitle);
        AppendRow(sb, "Company", model.CompanyName);
        AppendRow(sb, "Date", model.StartUtc.ToString("dddd, d MMMM yyyy", CultureInfo.InvariantCulture));
        AppendRow(sb, "Time", $"{model.StartUtc:HH:mm} UTC");
        AppendRow(sb, "Duration", $"{model.DurationMinutes} minutes");

        if (!string.IsNullOrWhiteSpace(model.MeetingLink))
        {
            AppendRow(sb, "Meeting link", model.MeetingLink, isLink: true);
        }
        else if (!string.IsNullOrWhiteSpace(model.Location))
        {
            AppendRow(sb, "Location", model.Location);
        }

        if (!string.IsNullOrWhiteSpace(model.Notes))
        {
            AppendRow(sb, "Notes", model.Notes);
        }

        sb.Append("</table>");

        // Calendar CTA.
        sb.Append("<table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"margin:0 0 18px;\"><tr>")
          .Append("<td style=\"background:#1a73e8;border-radius:8px;\">")
          .Append("<a href=\"").Append(Encode(model.GoogleCalendarUrl)).Append("\" ")
          .Append("style=\"display:inline-block;padding:12px 20px;font-size:14px;font-weight:600;color:#ffffff;text-decoration:none;\">")
          .Append("Add to Google Calendar</a></td>")
          .Append("</tr></table>");

        sb.Append("</td></tr><tr><td style=\"padding:16px 32px;background:#fafafa;border-top:1px solid ")
          .Append(Border).Append(";font-size:12px;color:").Append(Muted)
          .Append(";\">You are receiving this because of activity on your ").Append(Brand)
          .Append(" account.</td></tr></table></div>");

        return sb.ToString();
    }

    private static void AppendRow(StringBuilder sb, string label, string value, bool isLink = false)
    {
        sb.Append("<tr><td style=\"padding:7px 0;font-size:13px;color:").Append(Muted)
          .Append(";width:132px;vertical-align:top;\">").Append(Encode(label)).Append("</td>")
          .Append("<td style=\"padding:7px 0;font-size:14px;color:").Append(Ink)
          .Append(";font-weight:600;vertical-align:top;word-break:break-word;\">");

        if (isLink)
        {
            sb.Append("<a href=\"").Append(Encode(value)).Append("\" style=\"color:").Append(Accent)
              .Append(";text-decoration:underline;\">").Append(Encode(value)).Append("</a>");
        }
        else
        {
            sb.Append(Encode(value));
        }

        sb.Append("</td></tr>");
    }

    /// <summary>Generic notification email used by the in-app notification fan-out.</summary>
    public static string Notification(string title, string message, string? link)
        => Build(title, message, link is null ? null : "View in GetCareers", link);

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
