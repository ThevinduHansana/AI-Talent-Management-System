namespace RecruitmentPlatform.Application.Interfaces.Services;

/// <summary>
/// AI matching abstraction. The default implementation is a deterministic skill-overlap heuristic;
/// it can be swapped for an OpenAI/Azure AI-backed implementation without changing callers.
/// </summary>
public interface IMatchingService
{
    /// <summary>Returns a 0-100 fit score for a candidate against a job.</summary>
    Task<double> ScoreCandidateForJobAsync(Guid candidateId, Guid jobId, CancellationToken cancellationToken = default);
}
