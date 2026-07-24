using RecruitmentPlatform.Domain.Common;
using RecruitmentPlatform.Domain.Enums;

namespace RecruitmentPlatform.Domain.Entities;

/// <summary>
/// A skill possessed by a candidate, with self-reported proficiency.
/// </summary>
public class CandidateSkill : BaseEntity
{
    public Guid CandidateId { get; set; }

    public Candidate Candidate { get; set; } = null!;

    public Guid SkillId { get; set; }

    public Skill Skill { get; set; } = null!;

    public ProficiencyLevel ProficiencyLevel { get; set; } = ProficiencyLevel.Intermediate;

    public int YearsOfExperience { get; set; }
}
