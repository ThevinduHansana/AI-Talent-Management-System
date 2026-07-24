using RecruitmentPlatform.Domain.Common;

namespace RecruitmentPlatform.Domain.Entities;

/// <summary>
/// A professional certificate on a candidate's profile, optionally with an uploaded document.
/// </summary>
public class Certificate : BaseEntity
{
    public Guid CandidateId { get; set; }

    public Candidate Candidate { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public string? IssuingOrganization { get; set; }

    public DateTime? IssueDate { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public string? CredentialId { get; set; }

    public string? CredentialUrl { get; set; }

    // Optional uploaded document metadata
    public string? FileName { get; set; }

    public string? StoredFileName { get; set; }

    public string? FilePath { get; set; }

    public string? ContentType { get; set; }

    public long? FileSize { get; set; }
}
