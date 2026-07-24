using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RecruitmentPlatform.Application.Interfaces.Infrastructure;
using RecruitmentPlatform.Application.Interfaces.Repositories;
using RecruitmentPlatform.Application.Interfaces.Services;
using RecruitmentPlatform.Domain.Entities;

namespace RecruitmentPlatform.Application.Services;

/// <summary>
/// Candidate/job fit scoring backed by Claude. The model reads the full job posting and candidate
/// profile — free-text requirements, experience history and adjacent skills included — rather than
/// only exact skill-id overlap. Falls back to <see cref="HeuristicMatchingService"/> whenever the
/// provider is unconfigured, times out or returns something unusable.
/// </summary>
public class ClaudeMatchingService : IMatchingService
{
    private readonly IUnitOfWork _uow;
    private readonly IAiCompletionService _ai;
    private readonly HeuristicMatchingService _fallback;
    private readonly ILogger<ClaudeMatchingService> _logger;

    public ClaudeMatchingService(
        IUnitOfWork uow,
        IAiCompletionService ai,
        HeuristicMatchingService fallback,
        ILogger<ClaudeMatchingService> logger)
    {
        _uow = uow;
        _ai = ai;
        _fallback = fallback;
        _logger = logger;
    }

    public async Task<double> ScoreCandidateForJobAsync(Guid candidateId, Guid jobId, CancellationToken cancellationToken = default)
    {
        if (!_ai.IsEnabled)
        {
            return await _fallback.ScoreCandidateForJobAsync(candidateId, jobId, cancellationToken);
        }

        var job = await LoadJobAsync(jobId, cancellationToken);
        var candidate = await LoadCandidateAsync(candidateId, cancellationToken);
        if (job is null || candidate is null)
        {
            return await _fallback.ScoreCandidateForJobAsync(candidateId, jobId, cancellationToken);
        }

        var userPrompt = $"""
            # Job
            {AiPrompts.DescribeJob(job)}

            # Candidate
            {AiPrompts.DescribeCandidate(candidate)}

            Score this candidate's fit for this job.
            """;

        var json = await _ai.CompleteJsonAsync(
            new AiJsonRequest(AiPrompts.MatchingSystem, userPrompt, AiPrompts.MatchScoreSchema, MaxTokens: 4000),
            cancellationToken);

        var parsed = AiJson.Deserialize<MatchScoreResponse>(json);
        if (parsed is null)
        {
            _logger.LogInformation(
                "Falling back to heuristic scoring for candidate {CandidateId} on job {JobId}.", candidateId, jobId);
            return await _fallback.ScoreCandidateForJobAsync(candidateId, jobId, cancellationToken);
        }

        var score = Math.Round(Math.Clamp(parsed.Score, 0, 100), 1);
        _logger.LogDebug(
            "Scored candidate {CandidateId} at {Score} for job {JobId}: {Reasoning}",
            candidateId, score, jobId, parsed.Reasoning);

        return score;
    }

    private Task<Job?> LoadJobAsync(Guid jobId, CancellationToken cancellationToken) =>
        _uow.Jobs.Query()
            .Include(j => j.Organization)
            .Include(j => j.Department)
            .Include(j => j.JobSkills).ThenInclude(js => js.Skill)
            .FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);

    private Task<Candidate?> LoadCandidateAsync(Guid candidateId, CancellationToken cancellationToken) =>
        _uow.Candidates.Query()
            .Include(c => c.CandidateSkills).ThenInclude(cs => cs.Skill)
            .Include(c => c.Experiences)
            .Include(c => c.Educations)
            .FirstOrDefaultAsync(c => c.Id == candidateId, cancellationToken);
}

/// <summary>Shape of the model's reply to a fit-scoring prompt.</summary>
internal record MatchScoreResponse(
    [property: JsonPropertyName("score")] double Score,
    [property: JsonPropertyName("reasoning")] string Reasoning,
    [property: JsonPropertyName("matched_skills")] IReadOnlyList<string> MatchedSkills,
    [property: JsonPropertyName("missing_skills")] IReadOnlyList<string> MissingSkills);

/// <summary>Lenient deserialization of model output — a malformed reply is a fallback, not a crash.</summary>
internal static class AiJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static T? Deserialize<T>(string? json) where T : class
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            return JsonSerializer.Deserialize<T>(json, Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
