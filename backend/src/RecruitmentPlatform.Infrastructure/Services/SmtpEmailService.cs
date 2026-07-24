using System.Text.RegularExpressions;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using RecruitmentPlatform.Application.Common.Models;
using RecruitmentPlatform.Application.Interfaces.Infrastructure;

namespace RecruitmentPlatform.Infrastructure.Services;

/// <summary>
/// Sends transactional email over SMTP via MailKit. When no host is configured the call degrades
/// to a log line instead of throwing, so development and test environments need no mail server.
/// </summary>
public partial class SmtpEmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<EmailSettings> options, ILogger<SmtpEmailService> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
        => SendAsync(to, subject, htmlBody, Array.Empty<EmailAttachment>(), cancellationToken);

    public async Task SendAsync(string to, string subject, string htmlBody,
        IReadOnlyCollection<EmailAttachment> attachments, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(to))
        {
            _logger.LogWarning("EMAIL skipped: no recipient address for '{Subject}'.", subject);
            return;
        }

        if (!_settings.IsConfigured)
        {
            _logger.LogInformation("EMAIL (not configured, logged only) -> {To} | {Subject}", to, subject);
            return;
        }

        var sender = _settings.ResolvedFromAddress;
        if (string.IsNullOrWhiteSpace(sender))
        {
            _logger.LogError("EMAIL skipped: no sender address configured (set Email:FromAddress or Email:Username).");
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, sender));
        message.Subject = subject;

        try
        {
            message.To.Add(MailboxAddress.Parse(to));
        }
        catch (ParseException)
        {
            // A malformed address is bad data, not an outage — drop it rather than failing the caller.
            _logger.LogWarning("EMAIL skipped: '{To}' is not a valid address.", to);
            return;
        }

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = htmlBody,
            // A text/plain alternative keeps the message out of spam filters and readable in
            // clients with HTML disabled.
            TextBody = ToPlainText(htmlBody),
        };

        foreach (var attachment in attachments)
        {
            // Parse the declared MIME type so parameters such as `method=REQUEST` survive — some
            // clients only treat a .ics as an actionable invitation when that parameter is present.
            var contentType = ContentType.TryParse(attachment.ContentType, out var parsed)
                ? parsed
                : new ContentType("application", "octet-stream");

            bodyBuilder.Attachments.Add(attachment.FileName, attachment.Content, contentType);
        }

        message.Body = bodyBuilder.ToMessageBody();

        // Bound the whole exchange so a black-holed mail server cannot hang the request thread.
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(_settings.TimeoutSeconds));
        var token = timeoutCts.Token;

        using var client = new SmtpClient
        {
            Timeout = _settings.TimeoutSeconds * 1000,
            CheckCertificateRevocation = _settings.CheckCertificateRevocation,
        };

        // Port 465 is implicit TLS; 587 (and most others) negotiate STARTTLS after connecting.
        var security = _settings.Port == 465
            ? SecureSocketOptions.SslOnConnect
            : _settings.UseStartTls
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.Auto;

        await client.ConnectAsync(_settings.Host, _settings.Port, security, token);

        if (!string.IsNullOrWhiteSpace(_settings.Username))
        {
            await client.AuthenticateAsync(_settings.Username, _settings.Password ?? string.Empty, token);
        }

        await client.SendAsync(message, token);
        await client.DisconnectAsync(true, token);

        if (attachments.Count > 0)
        {
            _logger.LogInformation("EMAIL sent -> {To} | {Subject} | {Count} attachment(s)", to, subject, attachments.Count);
        }
        else
        {
            _logger.LogInformation("EMAIL sent -> {To} | {Subject}", to, subject);
        }
    }

    /// <summary>
    /// Derives a readable text/plain alternative from the HTML body. Intentionally simple — the
    /// templates are ours and structurally predictable, so a full HTML parser would be overkill.
    /// </summary>
    private static string ToPlainText(string html)
    {
        var text = BlockBreakRegex().Replace(html, "\n");
        text = TagRegex().Replace(text, string.Empty);
        text = System.Net.WebUtility.HtmlDecode(text);
        text = ExcessBlankLineRegex().Replace(text, "\n\n");
        return text.Trim();
    }

    [GeneratedRegex(@"</(p|div|tr|h1|h2|h3)>|<br\s*/?>", RegexOptions.IgnoreCase)]
    private static partial Regex BlockBreakRegex();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex TagRegex();

    [GeneratedRegex(@"[ \t]*\n\s*\n\s*")]
    private static partial Regex ExcessBlankLineRegex();
}
