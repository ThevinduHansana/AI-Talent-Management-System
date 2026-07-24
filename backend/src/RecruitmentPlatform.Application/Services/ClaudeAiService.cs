using System.Text;
using System.Text.Json.Serialization;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RecruitmentPlatform.Application.Common.Models;
using RecruitmentPlatform.Application.DTOs.Ai;
using RecruitmentPlatform.Application.DTOs.Jobs;
using RecruitmentPlatform.Application.Interfaces.Infrastructure;
using RecruitmentPlatform.Application.Interfaces.Repositories;
using RecruitmentPlatform.Application.Interfaces.Services;
using RecruitmentPlatform.Domain.Entities;
using RecruitmentPlatform.Domain.Enums;

namespace RecruitmentPlatform.Application.Services;

/// <summary>
/// Claude-backed implementation of <see cref="IAiService"/>: resume analysis and skill extraction,
/// job-recommendation generation and automated application feedback.
///
/// Every path degrades to <see cref="AiResumeService"/>'s deterministic heuristics when no API key
/// is configured or a model call fails, so the endpoints behave identically without credentials —
/// only the quality of the output changes.
/// </summary>
public class ClaudeAiService : IAiService
{
    private readonly IUnitOfWork _uow;
    private readonly IAiCompletionService _ai;
    private readonly AiResumeService _fallback;
    private readonly HeuristicMatchingService _prefilter;
    private readonly IMapper _mapper;
    private readonly AiSettings _settings;
    private readonly ILogger<ClaudeAiService> _logger;

    public ClaudeAiService(
        IUnitOfWork uow,
        IAiCompletionService ai,
        AiResumeService fallback,
        HeuristicMatchingService prefilter,
        IMapper mapper,
        IOptions<AiSettings> settings,
        ILogger<ClaudeAiService> logger)
    {
        _uow = uow;
        _ai = ai;
        _fallback = fallback;
        _prefilter = prefilter;
        _mapper = mapper;
        _settings = settings.Value;
        _logger = logger;
    }

    // ------------------------------------------------------------------ resume analysis

    public async Task<ResumeAnalysisResultDto> AnalyzeResumeAsync(Guid userId, Guid resumeId, bool autoAddSkills, CancellationToken cancellationToken = default)
    {
        // The heuristic pass does the document work (extraction, word count, parsed-text storage)
        // and doubles as the fallback result. Skills are added afterwards, from whichever engine wins.
        var baseline = await _fallback.AnalyzeResumeAsync(userId, resumeId, autoAddSkills: false, cancellationToken);

        var analysis = _ai.IsEnabled
            ? await AnalyzeWithModelAsync(resumeId, cancellationToken)
            : null;

        var detectedSkills = analysis is { Skills.Count: > 0 }
            ? Normalize(analysis.Skills)
            : baseline.DetectedSkills;

        IReadOnlyList<string> addedSkills = Array.Empty<string>();
        if (autoAddSkills && detectedSkills.Count > 0)
        {
            var candidate = await GetCandidateAsync(userId, cancellationToken);
            addedSkills = await CandidateSkillWriter.AddSkillsToProfileAsync(_uow, candidate.Id, detectedSkills, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);
        }

        if (analysis is null)
        {
            return baseline with { DetectedSkills = detectedSkills, SkillsAddedToProfile = addedSkills };
        }

        return baseline with
        {
            DetectedSkills = detectedSkills,
            SkillsAddedToProfile = addedSkills,
            DetectedSections = analysis.Sections.Count > 0 ? analysis.Sections : baseline.DetectedSections,
            CompletenessScore = Math.Clamp(analysis.CompletenessScore, 0, 100),
            Insights = analysis.Insights.Count > 0 ? analysis.Insights : baseline.Insights,
            Summary = analysis.Summary,
            Strengths = analysis.Strengths,
            Gaps = analysis.Gaps,
            SuggestedRoles = analysis.SuggestedRoles,
            Source = AiSource.Model,
        };
    }

    private async Task<ResumeAnalysisResponse?> AnalyzeWithModelAsync(Guid resumeId, CancellationToken cancellationToken)
    {
        // AnalyzeResumeAsync above has just persisted the extracted text.
        var resumeText = await _uow.Repository<Resume>().Query()
            .Where(r => r.Id == resumeId)
            .Select(r => r.ParsedText)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(resumeText)) return null;

        var catalogue = await _uow.Skills.Query()
            .OrderBy(s => s.Name)
            .Select(s => s.Name)
            .Take(300)
            .ToListAsync(cancellationToken);

        var userPrompt = $"""
            # Skill catalogue (prefer these exact names when the resume means the same skill)
            {string.Join(", ", catalogue)}

            # Resume
            {AiPrompts.Trim(resumeText, _settings.MaxResumeCharacters)}

            Analyse this resume.
            """;

        var json = await _ai.CompleteJsonAsync(
            new AiJsonRequest(AiPrompts.ResumeAnalystSystem, userPrompt, AiPrompts.ResumeAnalysisSchema),
            cancellationToken);

        var parsed = AiJson.Deserialize<ResumeAnalysisResponse>(json);
        if (parsed is null)
        {
            _logger.LogInformation("Falling back to heuristic resume analysis for resume {ResumeId}.", resumeId);
        }

        return parsed;
    }

    // ------------------------------------------------------------------ job recommendations

    public async Task<IReadOnlyList<JobRecommendationDto>> RecommendJobsAsync(Guid userId, int count, CancellationToken cancellationToken = default)
    {
        if (!_ai.IsEnabled)
        {
            return await _fallback.RecommendJobsAsync(userId, count, cancellationToken);
        }

        var take = count < 1 ? 5 : count;
        var candidate = await GetFullCandidateAsync(userId, cancellationToken);

        var appliedJobIds = (await _uow.Applications.Query()
            .Where(a => a.CandidateId == candidate.Id)
            .Select(a => a.JobId)
            .ToListAsync(cancellationToken)).ToHashSet();

        var openJobs = await _uow.Jobs.Query()
            .Where(j => j.Status == JobStatus.Open)
            .Include(j => j.Organization)
            .Include(j => j.Department)
            .Include(j => j.JobSkills).ThenInclude(js => js.Skill)
            .ToListAsync(cancellationToken);

        var eligible = openJobs.Where(j => !appliedJobIds.Contains(j.Id)).ToList();
        if (eligible.Count == 0) return Array.Empty<JobRecommendationDto>();

        var candidateSkillIds = candidate.CandidateSkills.Select(cs => cs.SkillId).ToHashSet();

        // Cheap deterministic retrieval first, then one model call to re-rank the shortlist —
        // this keeps recommendations to a single request regardless of how many jobs are open.
        var shortlistSize = Math.Clamp(take * 3, 8, 25);
        var shortlist = new List<(Job Job, double Score)>();
        foreach (var job in eligible)
        {
            var score = await _prefilter.ScoreCandidateForJobAsync(candidate.Id, job.Id, cancellationToken);
            shortlist.Add((job, score));
        }

        var ranked = shortlist
            .OrderByDescending(entry => entry.Score)
            .Take(shortlistSize)
            .ToList();

        var reranked = await RerankAsync(candidate, ranked.Select(entry => entry.Job).ToList(), cancellationToken);

        IEnumerable<(Job Job, double Score, string? Rationale)> results = reranked is null
            ? ranked.Select(entry => (entry.Job, entry.Score, (string?)null))
            : reranked;

        return results
            .Take(take)
            .Select(entry => new JobRecommendationDto(
                _mapper.Map<JobListItemDto>(entry.Job),
                entry.Score,
                entry.Job.JobSkills.Where(js => candidateSkillIds.Contains(js.SkillId)).Select(js => js.Skill.Name).ToList(),
                entry.Rationale,
                reranked is null ? AiSource.Heuristic : AiSource.Model))
            .ToList();
    }

    private async Task<List<(Job Job, double Score, string? Rationale)>?> RerankAsync(
        Candidate candidate, IReadOnlyList<Job> shortlist, CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Candidate");
        builder.AppendLine(AiPrompts.DescribeCandidate(candidate));
        builder.AppendLine("# Open roles");
        foreach (var job in shortlist)
        {
            builder.AppendLine($"## job_id: {job.Id}");
            builder.AppendLine(AiPrompts.DescribeJob(job, includeFullDescription: false));
            builder.AppendLine($"Description: {AiPrompts.Trim(job.Description, 700)}");
            builder.AppendLine();
        }
        builder.AppendLine("Rank every role above for this candidate.");

        var json = await _ai.CompleteJsonAsync(
            new AiJsonRequest(AiPrompts.RecommendationSystem, builder.ToString(), AiPrompts.RecommendationSchema, MaxTokens: 8000),
            cancellationToken);

        var parsed = AiJson.Deserialize<RecommendationListResponse>(json);
        if (parsed is null || parsed.Recommendations.Count == 0)
        {
            _logger.LogInformation("Falling back to heuristic job ranking for candidate {CandidateId}.", candidate.Id);
            return null;
        }

        var byId = shortlist.ToDictionary(job => job.Id);
        var results = new List<(Job, double, string?)>();
        var seen = new HashSet<Guid>();

        foreach (var recommendation in parsed.Recommendations)
        {
            // The model echoes ids back; ignore anything it invented or repeated.
            if (!Guid.TryParse(recommendation.JobId, out var jobId)) continue;
            if (!byId.TryGetValue(jobId, out var job) || !seen.Add(jobId)) continue;

            results.Add((job, Math.Round(Math.Clamp(recommendation.Score, 0, 100), 1), recommendation.Rationale));
        }

        return results.Count == 0 ? null : results;
    }

    // ------------------------------------------------------------------ application feedback

    public async Task<ApplicationFeedbackDto> GenerateApplicationFeedbackAsync(Guid userId, Guid applicationId, CancellationToken cancellationToken = default)
    {
        if (!_ai.IsEnabled)
        {
            return await _fallback.GenerateApplicationFeedbackAsync(userId, applicationId, cancellationToken);
        }

        var (application, _) = await _fallback.LoadFeedbackContextAsync(userId, applicationId, cancellationToken);

        var resumeText = application.ResumeId is null
            ? null
            : await _uow.Repository<Resume>().Query()
                .Where(r => r.Id == application.ResumeId)
                .Select(r => r.ParsedText)
                .FirstOrDefaultAsync(cancellationToken);

        var userPrompt = $"""
            # Job the candidate applied to
            {AiPrompts.DescribeJob(application.Job)}

            # Candidate profile
            {AiPrompts.DescribeCandidate(application.Candidate, AiPrompts.Trim(resumeText, _settings.MaxResumeCharacters))}

            Write feedback for this candidate on this application.
            """;

        var json = await _ai.CompleteJsonAsync(
            new AiJsonRequest(AiPrompts.FeedbackSystem, userPrompt, AiPrompts.FeedbackSchema),
            cancellationToken);

        var parsed = AiJson.Deserialize<FeedbackResponse>(json);
        if (parsed is null)
        {
            _logger.LogInformation("Falling back to heuristic feedback for application {ApplicationId}.", applicationId);
            return await _fallback.GenerateApplicationFeedbackAsync(userId, applicationId, cancellationToken);
        }

        return new ApplicationFeedbackDto(
            application.Id,
            application.JobId,
            application.Job.Title,
            application.Job.Organization.Name,
            application.Status,
            Math.Round(Math.Clamp(parsed.Score, 0, 100), 1),
            parsed.Summary,
            parsed.Strengths,
            parsed.Gaps,
            parsed.Recommendations,
            AiSource.Model);
    }

    // ------------------------------------------------------------------ helpers

    private static IReadOnlyList<string> Normalize(IReadOnlyList<string> skills) => skills
        .Select(s => s.Trim())
        .Where(s => s.Length is > 0 and <= 100)
        .GroupBy(s => s.ToLowerInvariant())
        .Select(group => group.First())
        .OrderBy(s => s)
        .ToList();

    private async Task<Candidate> GetCandidateAsync(Guid userId, CancellationToken cancellationToken)
        => await _uow.Candidates.GetByUserIdAsync(userId, cancellationToken)
           ?? throw new Common.Exceptions.NotFoundException("Candidate profile", userId);

    private async Task<Candidate> GetFullCandidateAsync(Guid userId, CancellationToken cancellationToken)
        => await _uow.Candidates.Query()
               .Include(c => c.CandidateSkills).ThenInclude(cs => cs.Skill)
               .Include(c => c.Experiences)
               .Include(c => c.Educations)
               .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken)
           ?? throw new Common.Exceptions.NotFoundException("Candidate profile", userId);
}

// ---------------------------------------------------------------------- model response shapes

internal record ResumeAnalysisResponse(
    [property: JsonPropertyName("summary")] string Summary,
    [property: JsonPropertyName("skills")] IReadOnlyList<string> Skills,
    [property: JsonPropertyName("sections")] IReadOnlyList<string> Sections,
    [property: JsonPropertyName("completeness_score")] int CompletenessScore,
    [property: JsonPropertyName("insights")] IReadOnlyList<string> Insights,
    [property: JsonPropertyName("strengths")] IReadOnlyList<string> Strengths,
    [property: JsonPropertyName("gaps")] IReadOnlyList<string> Gaps,
    [property: JsonPropertyName("suggested_roles")] IReadOnlyList<string> SuggestedRoles);

internal record RecommendationListResponse(
    [property: JsonPropertyName("recommendations")] IReadOnlyList<RecommendationEntry> Recommendations);

internal record RecommendationEntry(
    [property: JsonPropertyName("job_id")] string JobId,
    [property: JsonPropertyName("score")] double Score,
    [property: JsonPropertyName("rationale")] string Rationale);

internal record FeedbackResponse(
    [property: JsonPropertyName("score")] double Score,
    [property: JsonPropertyName("summary")] string Summary,
    [property: JsonPropertyName("strengths")] IReadOnlyList<string> Strengths,
    [property: JsonPropertyName("gaps")] IReadOnlyList<string> Gaps,
    [property: JsonPropertyName("recommendations")] IReadOnlyList<string> Recommendations);
