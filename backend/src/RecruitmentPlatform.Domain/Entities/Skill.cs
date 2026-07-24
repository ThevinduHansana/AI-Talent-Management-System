using RecruitmentPlatform.Domain.Common;

namespace RecruitmentPlatform.Domain.Entities;

/// <summary>
/// A named skill in the master skills catalog, referenced by both jobs and candidates.
/// </summary>
public class Skill : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string NormalizedName { get; set; } = string.Empty;

    public string Category { get; set; } = "General";

    public ICollection<CandidateSkill> CandidateSkills { get; set; } = new List<CandidateSkill>();

    public ICollection<JobSkill> JobSkills { get; set; } = new List<JobSkill>();
}
