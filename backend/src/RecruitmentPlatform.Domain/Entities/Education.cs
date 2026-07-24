using RecruitmentPlatform.Domain.Common;

namespace RecruitmentPlatform.Domain.Entities;

/// <summary>
/// An education record on a candidate's profile.
/// </summary>
public class Education : BaseEntity
{
    public Guid CandidateId { get; set; }

    public Candidate Candidate { get; set; } = null!;

    public string Institution { get; set; } = string.Empty;

    public string Degree { get; set; } = string.Empty;

    public string? FieldOfStudy { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public bool IsCurrent { get; set; }

    public string? Grade { get; set; }

    public string? Description { get; set; }
}
