using Microsoft.EntityFrameworkCore;
using RecruitmentPlatform.Application.Interfaces.Repositories;
using RecruitmentPlatform.Domain.Entities;
using RecruitmentPlatform.Domain.Enums;

namespace RecruitmentPlatform.Application.Services;

/// <summary>
/// Adds extracted skills to a candidate's profile, creating catalogue entries for names that are
/// new. Shared by the heuristic and Claude-backed resume analysers so both write identically.
/// </summary>
internal static class CandidateSkillWriter
{
    /// <summary>
    /// Attaches <paramref name="skillNames"/> to the candidate and returns the names actually
    /// added. Changes are staged only — the caller owns <c>SaveChangesAsync</c>.
    /// </summary>
    public static async Task<List<string>> AddSkillsToProfileAsync(
        IUnitOfWork uow, Guid candidateId, IReadOnlyList<string> skillNames, CancellationToken cancellationToken)
    {
        var existingSkillIds = (await uow.Repository<CandidateSkill>().Query()
            .Where(cs => cs.CandidateId == candidateId)
            .Select(cs => cs.SkillId)
            .ToListAsync(cancellationToken)).ToHashSet();

        var added = new List<string>();
        foreach (var name in skillNames)
        {
            var trimmed = name.Trim();
            if (trimmed.Length == 0) continue;

            var normalized = trimmed.ToUpperInvariant();
            var skill = await uow.Skills.GetByNormalizedNameAsync(normalized, cancellationToken);
            if (skill is null)
            {
                skill = new Skill { Name = trimmed, NormalizedName = normalized, Category = "Extracted" };
                await uow.Skills.AddAsync(skill, cancellationToken);
            }

            if (existingSkillIds.Contains(skill.Id)) continue;

            await uow.Repository<CandidateSkill>().AddAsync(new CandidateSkill
            {
                CandidateId = candidateId,
                Skill = skill,
                ProficiencyLevel = ProficiencyLevel.Intermediate,
                YearsOfExperience = 0,
            }, cancellationToken);
            existingSkillIds.Add(skill.Id);
            added.Add(skill.Name);
        }

        return added;
    }
}
