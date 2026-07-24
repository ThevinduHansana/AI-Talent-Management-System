namespace RecruitmentPlatform.Application.Common.Models;

/// <summary>
/// SMTP configuration for transactional email. When <see cref="IsConfigured"/> is false the
/// platform falls back to the logging channel, so the app runs unchanged without a mail server.
/// </summary>
public class EmailSettings
{
    public const string SectionName = "Email";

    /// <summary>Master switch. Set to false to force the logging channel.</summary>
    public bool Enabled { get; set; } = true;

    public string? Host { get; set; }

    public int Port { get; set; } = 587;

    /// <summary>
    /// Upgrade the connection with STARTTLS (the usual choice on port 587). Port 465 implies
    /// implicit TLS and is detected automatically regardless of this flag.
    /// </summary>
    public bool UseStartTls { get; set; } = true;

    public string? Username { get; set; }

    public string? Password { get; set; }

    public string FromAddress { get; set; } = "no-reply@getcareers.local";

    public string FromName { get; set; } = "GetCareers";

    /// <summary>
    /// The address messages are actually sent from. Providers that authenticate a mailbox (Gmail,
    /// Outlook) reject or silently rewrite a From that isn't the signed-in account, so the
    /// username wins when it looks like an address and no explicit sender was configured.
    /// </summary>
    public string ResolvedFromAddress =>
        string.IsNullOrWhiteSpace(FromAddress) || FromAddress == "no-reply@getcareers.local"
            ? (Username?.Contains('@') == true ? Username : FromAddress)
            : FromAddress;

    /// <summary>Public origin of the React client, used to build links inside emails.</summary>
    public string AppBaseUrl { get; set; } = "http://localhost:5173";

    /// <summary>Per-message wall-clock budget, so a hung mail server cannot stall a request.</summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Check the server certificate against its revocation list (OCSP/CRL). Defaults to on.
    /// Networks that block those endpoints make the lookup fail even for a perfectly valid
    /// certificate, which aborts the TLS handshake; set this false there. The certificate chain
    /// and host name are still fully validated either way — only the revocation lookup is skipped.
    /// </summary>
    public bool CheckCertificateRevocation { get; set; } = true;

    /// <summary>True when a real SMTP host is available to send through.</summary>
    public bool IsConfigured => Enabled && !string.IsNullOrWhiteSpace(Host);
}
