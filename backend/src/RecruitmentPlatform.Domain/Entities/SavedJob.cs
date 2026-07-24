using RecruitmentPlatform.Domain.Common;

namespace RecruitmentPlatform.Domain.Entities;

/// <summary>
/// A job bookmarked by a candidate for later.
/// </summary>
public class SavedJob : BaseEntity
{
    public Guid CandidateId { get; set; }

    public Candidate Candidate { get; set; } = null!;

    public Guid JobId { get; set; }

    public Job Job { get; set; } = null!;

    public DateTime SavedAt { get; set; } = DateTime.UtcNow;
}
