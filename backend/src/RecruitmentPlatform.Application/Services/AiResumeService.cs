using System.Text.RegularExpressions;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RecruitmentPlatform.Application.Common.Exceptions;
using RecruitmentPlatform.Application.DTOs.Ai;
using RecruitmentPlatform.Application.DTOs.Jobs;
using RecruitmentPlatform.Application.Interfaces.Infrastructure;
using RecruitmentPlatform.Application.Interfaces.Repositories;
using RecruitmentPlatform.Application.Interfaces.Services;
using RecruitmentPlatform.Domain.Entities;
using RecruitmentPlatform.Domain.Enums;

namespace RecruitmentPlatform.Application.Services;

/// <summary>
/// Deterministic resume analysis and job recommendation: real PDF/DOCX text extraction plus a
/// keyword/skill-overlap heuristic. This is the offline fallback behind <see cref="IAiService"/> —
/// <see cref="ClaudeAiService"/> delegates here whenever no Anthropic API key is configured or a
/// model call fails, so the feature never goes dark.
/// </summary>
public class AiResumeService : IAiService
{
    // Common skills used to enrich detection beyond the seeded catalog.
    private static readonly string[] KnownSkills =
    {
        "C#", ".NET", "ASP.NET Core", "Java", "Python", "JavaScript", "TypeScript", "React", "Angular",
        "Vue", "Node.js", "Express", "Go", "Rust", "SQL", "PostgreSQL", "MySQL", "MongoDB", "Redis",
        "Docker", "Kubernetes", "AWS", "Azure", "GCP", "GraphQL", "REST", "CI/CD", "Git", "Agile",
        "Scrum", "Machine Learning", "TensorFlow", "Communication", "Leadership",
    };

    private static readonly (string Keyword, string Section)[] SectionMarkers =
    {
        ("experience", "Experience"), ("work history", "Experience"), ("employment", "Experience"),
        ("education", "Education"), ("university", "Education"), ("degree", "Education"),
        ("skills", "Skills"), ("summary", "Summary"), ("objective", "Summary"),
        ("project", "Projects"), ("certification", "Certifications"),
    };

    private readonly IUnitOfWork _uow;
    private readonly IFileStorageService _storage;
    private readonly ITextExtractor _textExtractor;
    private readonly IMatchingService _matching;
    private readonly IMapper _mapper;

    public AiResumeService(IUnitOfWork uow, IFileStorageService storage, ITextExtractor textExtractor,
        IMatchingService matching, IMapper mapper)
    {
        _uow = uow;
        _storage = storage;
        _textExtractor = textExtractor;
        _matching = matching;
        _mapper = mapper;
    }

    public async Task<ResumeAnalysisResultDto> AnalyzeResumeAsync(Guid userId, Guid resumeId, bool autoAddSkills, CancellationToken cancellationToken = default)
    {
        var candidate = await GetCandidateAsync(userId, cancellationToken);

        var resume = await _uow.Repository<Resume>().Query().AsTracking()
            .FirstOrDefaultAsync(r => r.Id == resumeId && r.CandidateId == candidate.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Resume), resumeId);

        var stream = await _storage.GetAsync(resume.FilePath, cancellationToken)
            ?? throw new NotFoundException("Resume file", resumeId);

        string text;
        await using (stream)
        {
            text = await _textExtractor.ExtractTextAsync(stream, resume.ContentType, resume.FileName, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ValidationException("Resume", "Could not extract text from this document. PDF and DOCX are supported.");
        }

        resume.ParsedText = text.Length > 20000 ? text[..20000] : text;

        var wordCount = Regex.Matches(text, @"\b[\w'-]+\b").Count;

        // Build the detection vocabulary from the skill catalog plus well-known skills.
        var catalog = await _uow.Skills.Query().Select(s => new { s.Id, s.Name }).ToListAsync(cancellationToken);
        var vocabulary = catalog.Select(c => c.Name)
            .Concat(KnownSkills)
            .GroupBy(n => n.ToLowerInvariant())
            .Select(g => g.First())
            .ToList();

        var detectedSkills = DetectSkills(text, vocabulary);
        var detectedSections = DetectSections(text);

        var addedSkills = new List<string>();
        if (autoAddSkills && detectedSkills.Count > 0)
        {
            addedSkills = await CandidateSkillWriter.AddSkillsToProfileAsync(_uow, candidate.Id, detectedSkills, cancellationToken);
        }

        await _uow.SaveChangesAsync(cancellationToken);

        var completeness = ScoreCompleteness(wordCount, detectedSections.Count, detectedSkills.Count);
        var insights = BuildInsights(wordCount, detectedSections, detectedSkills.Count);

        return new ResumeAnalysisResultDto(resume.Id, resume.FileName, wordCount, detectedSkills,
            addedSkills, detectedSections, completeness, insights);
    }

    public async Task<IReadOnlyList<JobRecommendationDto>> RecommendJobsAsync(Guid userId, int count, CancellationToken cancellationToken = default)
    {
        var candidate = await GetCandidateAsync(userId, cancellationToken);

        var candidateSkillIds = (await _uow.Repository<CandidateSkill>().Query()
            .Where(cs => cs.CandidateId == candidate.Id)
            .Select(cs => cs.SkillId)
            .ToListAsync(cancellationToken)).ToHashSet();

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

        var candidates = openJobs.Where(j => !appliedJobIds.Contains(j.Id)).ToList();

        var recommendations = new List<JobRecommendationDto>();
        foreach (var job in candidates)
        {
            var score = await _matching.ScoreCandidateForJobAsync(candidate.Id, job.Id, cancellationToken);
            var matchingSkills = job.JobSkills
                .Where(js => candidateSkillIds.Contains(js.SkillId))
                .Select(js => js.Skill.Name)
                .ToList();

            recommendations.Add(new JobRecommendationDto(_mapper.Map<JobListItemDto>(job), score, matchingSkills));
        }

        return recommendations
            .OrderByDescending(r => r.MatchScore)
            .ThenByDescending(r => r.MatchingSkills.Count)
            .Take(count < 1 ? 5 : count)
            .ToList();
    }

    private static List<string> DetectSkills(string text, IEnumerable<string> vocabulary)
    {
        var found = new List<string>();
        foreach (var skill in vocabulary)
        {
            // Word-boundary match that respects symbols like C#, C++ and .NET.
            var pattern = $@"(?<![A-Za-z0-9+#.]){Regex.Escape(skill)}(?![A-Za-z0-9+#.])";
            if (Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase))
            {
                found.Add(skill);
            }
        }
        return found.OrderBy(s => s).ToList();
    }

    private static List<string> DetectSections(string text)
    {
        var lower = text.ToLowerInvariant();
        var sections = new HashSet<string>();
        if (lower.Contains('@') || Regex.IsMatch(text, @"\+?\d[\d\s().-]{7,}\d")) sections.Add("Contact");
        foreach (var (keyword, section) in SectionMarkers)
        {
            if (lower.Contains(keyword)) sections.Add(section);
        }
        return sections.OrderBy(s => s).ToList();
    }

    public async Task<ApplicationFeedbackDto> GenerateApplicationFeedbackAsync(Guid userId, Guid applicationId, CancellationToken cancellationToken = default)
    {
        var (application, candidateSkills) = await LoadFeedbackContextAsync(userId, applicationId, cancellationToken);

        var score = await _matching.ScoreCandidateForJobAsync(application.CandidateId, application.JobId, cancellationToken);

        var matched = application.Job.JobSkills
            .Where(js => candidateSkills.Contains(js.SkillId))
            .Select(js => js.Skill.Name)
            .ToList();
        var missing = application.Job.JobSkills
            .Where(js => !candidateSkills.Contains(js.SkillId))
            .OrderByDescending(js => js.Weight)
            .Select(js => js.Skill.Name)
            .ToList();

        var strengths = matched.Count > 0
            ? matched.Take(5).Select(s => $"Your profile lists {s}, which this role asks for.").ToList()
            : new List<string> { "You applied early — keep your profile updated while the role is open." };

        var gaps = missing.Take(5).Select(s => $"The role lists {s}, which your profile does not currently evidence.").ToList();
        if (gaps.Count == 0) gaps.Add("No required skill from this posting is missing from your profile.");

        var recommendations = new List<string>();
        if (missing.Count > 0) recommendations.Add($"Add evidence for {string.Join(", ", missing.Take(3))} to your profile or resume.");
        if (application.Candidate.Experiences.Count == 0) recommendations.Add("Add your work history — recruiters filter on relevant experience.");
        if (string.IsNullOrWhiteSpace(application.Candidate.Summary)) recommendations.Add("Write a short profile summary tailored to roles like this one.");
        if (recommendations.Count == 0) recommendations.Add("Your profile covers this role's requirements — keep it current and apply to similar openings.");

        var summary = $"Your profile matches roughly {score:0}% of what this role asks for, "
                      + $"covering {matched.Count} of {application.Job.JobSkills.Count} listed skills.";

        return new ApplicationFeedbackDto(
            application.Id,
            application.JobId,
            application.Job.Title,
            application.Job.Organization.Name,
            application.Status,
            score,
            summary,
            strengths,
            gaps,
            recommendations,
            AiSource.Heuristic);
    }

    /// <summary>
    /// Loads the application (ownership-checked against <paramref name="userId"/>) with the job,
    /// its skills and the candidate profile, plus the candidate's skill ids.
    /// </summary>
    internal async Task<(JobApplication Application, HashSet<Guid> CandidateSkillIds)> LoadFeedbackContextAsync(
        Guid userId, Guid applicationId, CancellationToken cancellationToken)
    {
        var candidate = await GetCandidateAsync(userId, cancellationToken);

        var application = await _uow.Applications.Query()
            .Include(a => a.Job).ThenInclude(j => j.Organization)
            .Include(a => a.Job).ThenInclude(j => j.Department)
            .Include(a => a.Job).ThenInclude(j => j.JobSkills).ThenInclude(js => js.Skill)
            .Include(a => a.Candidate).ThenInclude(c => c.CandidateSkills).ThenInclude(cs => cs.Skill)
            .Include(a => a.Candidate).ThenInclude(c => c.Experiences)
            .Include(a => a.Candidate).ThenInclude(c => c.Educations)
            .FirstOrDefaultAsync(a => a.Id == applicationId && a.CandidateId == candidate.Id, cancellationToken)
            ?? throw new NotFoundException("Application", applicationId);

        var candidateSkillIds = application.Candidate.CandidateSkills.Select(cs => cs.SkillId).ToHashSet();
        return (application, candidateSkillIds);
    }

    private static int ScoreCompleteness(int wordCount, int sectionCount, int skillCount)
    {
        // Sections contribute up to 60, content length up to 25, skills up to 15.
        var sectionScore = Math.Min(sectionCount, 6) / 6.0 * 60;
        var lengthScore = Math.Min(wordCount, 600) / 600.0 * 25;
        var skillScore = Math.Min(skillCount, 8) / 8.0 * 15;
        return (int)Math.Round(sectionScore + lengthScore + skillScore);
    }

    private static List<string> BuildInsights(int wordCount, IReadOnlyList<string> sections, int skillCount)
    {
        var insights = new List<string>();
        if (!sections.Contains("Summary")) insights.Add("Add a short professional summary at the top of your resume.");
        if (!sections.Contains("Skills")) insights.Add("Include a dedicated Skills section to improve matching.");
        if (!sections.Contains("Experience")) insights.Add("No work-experience section was detected.");
        if (!sections.Contains("Education")) insights.Add("No education section was detected.");
        if (wordCount < 200) insights.Add("Your resume looks short — consider adding more detail about your achievements.");
        if (skillCount >= 6) insights.Add($"Strong skills coverage — {skillCount} recognised skills detected.");
        if (insights.Count == 0) insights.Add("Your resume looks well-structured. Great job!");
        return insights;
    }

    private async Task<Candidate> GetCandidateAsync(Guid userId, CancellationToken cancellationToken)
        => await _uow.Candidates.GetByUserIdAsync(userId, cancellationToken)
           ?? throw new NotFoundException("Candidate profile", userId);
}
