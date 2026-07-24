using RecruitmentPlatform.Domain.Common;

namespace RecruitmentPlatform.Domain.Entities;

/// <summary>
/// An uploaded resume document. The binary is stored via the storage abstraction; only
/// metadata and the parsed text (for AI features) live in the database.
/// </summary>
public class Resume : BaseEntity
{
    public Guid CandidateId { get; set; }

    public Candidate Candidate { get; set; } = null!;

    public string FileName { get; set; } = string.Empty;

    public string StoredFileName { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public bool IsPrimary { get; set; }

    /// <summary>Plain text extracted from the document, used by AI parsing/matching.</summary>
    public string? ParsedText { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
