using RecruitmentPlatform.Domain.Common;
using RecruitmentPlatform.Domain.Enums;

namespace RecruitmentPlatform.Domain.Entities;

/// <summary>
/// A skill required or preferred by a job, with a weight used for AI candidate matching.
/// </summary>
public class JobSkill : BaseEntity
{
    public Guid JobId { get; set; }

    public Job Job { get; set; } = null!;

    public Guid SkillId { get; set; }

    public Skill Skill { get; set; } = null!;

    public bool IsRequired { get; set; } = true;

    public ProficiencyLevel MinimumProficiency { get; set; } = ProficiencyLevel.Intermediate;

    /// <summary>Relative importance (1-10) used when scoring candidate/job fit.</summary>
    public int Weight { get; set; } = 5;
}
