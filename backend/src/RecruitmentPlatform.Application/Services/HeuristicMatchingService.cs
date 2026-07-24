using Microsoft.EntityFrameworkCore;
using RecruitmentPlatform.Application.Interfaces.Repositories;
using RecruitmentPlatform.Application.Interfaces.Services;
using RecruitmentPlatform.Domain.Entities;

namespace RecruitmentPlatform.Application.Services;

/// <summary>
/// Deterministic candidate/job fit scoring based on weighted skill overlap and proficiency.
/// This stands in for a real AI model and implements the same <see cref="IMatchingService"/>
/// contract, so it can be replaced by an OpenAI/Azure AI implementation transparently.
/// </summary>
public class HeuristicMatchingService : IMatchingService
{
    private const double NoRequirementBaseline = 60d;

    private readonly IUnitOfWork _uow;

    public HeuristicMatchingService(IUnitOfWork uow) => _uow = uow;

    public async Task<double> ScoreCandidateForJobAsync(Guid candidateId, Guid jobId, CancellationToken cancellationToken = default)
    {
        var jobSkills = await _uow.Repository<JobSkill>().Query()
            .Where(js => js.JobId == jobId)
            .Select(js => new { js.SkillId, js.Weight, js.MinimumProficiency })
            .ToListAsync(cancellationToken);

        if (jobSkills.Count == 0)
        {
            return NoRequirementBaseline;
        }

        var candidateSkills = await _uow.Repository<CandidateSkill>().Query()
            .Where(cs => cs.CandidateId == candidateId)
            .Select(cs => new { cs.SkillId, cs.ProficiencyLevel })
            .ToListAsync(cancellationToken);

        var candidateBySkill = candidateSkills.ToDictionary(cs => cs.SkillId, cs => (int)cs.ProficiencyLevel);

        double totalWeight = 0;
        double credit = 0;

        foreach (var js in jobSkills)
        {
            var weight = Math.Max(1, js.Weight);
            totalWeight += weight;

            if (!candidateBySkill.TryGetValue(js.SkillId, out var candidateProficiency))
            {
                continue; // Candidate lacks this skill: no credit.
            }

            var required = (int)js.MinimumProficiency;
            // Full credit when the candidate meets/exceeds the required level; partial otherwise.
            var proficiencyFactor = required <= 0
                ? 1d
                : Math.Min(1d, (candidateProficiency + 1d) / (required + 1d));

            credit += weight * proficiencyFactor;
        }

        var score = credit / totalWeight * 100d;
        return Math.Round(Math.Clamp(score, 0, 100), 1);
    }
}
