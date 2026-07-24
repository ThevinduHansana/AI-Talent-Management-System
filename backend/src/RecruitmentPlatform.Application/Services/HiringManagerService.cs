using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RecruitmentPlatform.Application.Common.Exceptions;
using RecruitmentPlatform.Application.Common.Models;
using RecruitmentPlatform.Application.DTOs.Candidates;
using RecruitmentPlatform.Application.DTOs.HiringManager;
using RecruitmentPlatform.Application.Interfaces.Infrastructure;
using RecruitmentPlatform.Application.Interfaces.Repositories;
using RecruitmentPlatform.Application.Interfaces.Services;
using RecruitmentPlatform.Domain.Entities;
using RecruitmentPlatform.Domain.Enums;

namespace RecruitmentPlatform.Application.Services;

/// <summary>
/// Hiring-manager workflow, scoped to the manager's organization: reviewing candidates that
/// recruiters have advanced, recording evaluations and interview feedback, and deciding hires.
/// </summary>
public class HiringManagerService : IHiringManagerService
{
    private static readonly ApplicationStatus[] ReviewableStatuses =
    {
        ApplicationStatus.Shortlisted, ApplicationStatus.InterviewScheduled,
        ApplicationStatus.Interviewed, ApplicationStatus.Offered,
    };

    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly INotificationService _notifications;
    private readonly IFileStorageService _storage;
    private readonly IAuditService _audit;

    public HiringManagerService(IUnitOfWork uow, IMapper mapper, INotificationService notifications, IFileStorageService storage, IAuditService audit)
    {
        _uow = uow;
        _mapper = mapper;
        _notifications = notifications;
        _storage = storage;
        _audit = audit;
    }

    public async Task<HiringManagerDashboardDto> GetDashboardAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var hm = await GetHiringManagerAsync(userId, cancellationToken);
        var orgId = RequireOrganization(hm);

        var orgApps = _uow.Applications.Query().Where(a => a.Job.OrganizationId == orgId);

        var toReview = await orgApps.CountAsync(a =>
            a.Status == ApplicationStatus.Shortlisted ||
            a.Status == ApplicationStatus.InterviewScheduled ||
            a.Status == ApplicationStatus.Interviewed, cancellationToken);
        var hired = await orgApps.CountAsync(a => a.Status == ApplicationStatus.Hired, cancellationToken);
        var rejected = await orgApps.CountAsync(a => a.Status == ApplicationStatus.Rejected, cancellationToken);

        var evaluations = _uow.Repository<CandidateEvaluation>().Query().Where(e => e.HiringManagerId == hm.Id);
        var evaluated = await evaluations.CountAsync(cancellationToken);
        var pending = await evaluations.CountAsync(e => e.Decision == EvaluationDecision.Pending, cancellationToken);

        return new HiringManagerDashboardDto(toReview, evaluated, hired, rejected, pending);
    }

    public async Task<PagedResult<ReviewCandidateDto>> GetReviewQueueAsync(Guid userId, ReviewQueueQuery query, CancellationToken cancellationToken = default)
    {
        var hm = await GetHiringManagerAsync(userId, cancellationToken);
        var orgId = RequireOrganization(hm);

        var q = _uow.Applications.Query().Where(a => a.Job.OrganizationId == orgId);
        q = query.Status.HasValue
            ? q.Where(a => a.Status == query.Status.Value)
            : q.Where(a => ReviewableStatuses.Contains(a.Status));
        if (query.JobId.HasValue) q = q.Where(a => a.JobId == query.JobId.Value);

        q = q.OrderByDescending(a => a.MatchScore ?? -1).ThenByDescending(a => a.AppliedAt);

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(a => new ReviewCandidateDto(
                a.Id, a.JobId, a.Job.Title, a.CandidateId,
                a.Candidate.User.FirstName + " " + a.Candidate.User.LastName,
                a.Candidate.User.Email, a.Candidate.Headline, a.Status, a.MatchScore, a.AppliedAt,
                a.Evaluations.Any(e => e.HiringManagerId == hm.Id),
                a.Evaluations.Where(e => e.HiringManagerId == hm.Id).Select(e => (int?)e.OverallScore).FirstOrDefault(),
                a.Interviews.Count))
            .ToListAsync(cancellationToken);

        return new PagedResult<ReviewCandidateDto>(items, total, query.Page, query.PageSize);
    }

    public async Task<CandidateReviewDetailDto> GetCandidateDetailAsync(Guid userId, Guid applicationId, CancellationToken cancellationToken = default)
    {
        var hm = await GetHiringManagerAsync(userId, cancellationToken);
        var orgId = RequireOrganization(hm);

        var application = await _uow.Applications.Query()
            .Include(a => a.Job)
            .Include(a => a.Candidate).ThenInclude(c => c.User)
            .Include(a => a.Candidate).ThenInclude(c => c.CandidateSkills).ThenInclude(cs => cs.Skill)
            .Include(a => a.Candidate).ThenInclude(c => c.Educations)
            .Include(a => a.Candidate).ThenInclude(c => c.Experiences)
            .Include(a => a.Candidate).ThenInclude(c => c.Resumes)
            .Include(a => a.Interviews).ThenInclude(i => i.Feedbacks).ThenInclude(f => f.InterviewerUser)
            .Include(a => a.Evaluations).ThenInclude(e => e.HiringManager).ThenInclude(h => h.User)
            .FirstOrDefaultAsync(a => a.Id == applicationId && a.Job.OrganizationId == orgId, cancellationToken)
            ?? throw new NotFoundException("Application", applicationId);

        var candidate = application.Candidate;

        var interviews = application.Interviews
            .OrderBy(i => i.ScheduledAt)
            .Select(i => new InterviewSummaryDto(
                i.Id, i.Title, i.ScheduledAt, i.DurationMinutes, i.Mode, i.Status,
                i.Feedbacks.Select(ToFeedbackDto).ToList()))
            .ToList();

        var evaluation = application.Evaluations.FirstOrDefault(e => e.HiringManagerId == hm.Id);

        var resumes = candidate.Resumes
            .OrderByDescending(r => r.IsPrimary).ThenByDescending(r => r.UploadedAt)
            .Select(r => new ResumeDto(r.Id, r.FileName, r.ContentType, r.FileSize, r.IsPrimary, r.UploadedAt))
            .ToList();

        return new CandidateReviewDetailDto(
            application.Id, application.JobId, application.Job.Title, application.Status, application.MatchScore,
            application.CoverLetter, application.AppliedAt,
            candidate.Id, candidate.User.FirstName + " " + candidate.User.LastName, candidate.User.Email,
            candidate.Headline, candidate.Summary, candidate.Location, candidate.CurrentPosition, candidate.YearsOfExperience,
            _mapper.Map<List<CandidateSkillDto>>(candidate.CandidateSkills.OrderBy(s => s.Skill.Name)),
            _mapper.Map<List<EducationDto>>(candidate.Educations.OrderByDescending(e => e.StartDate)),
            _mapper.Map<List<ExperienceDto>>(candidate.Experiences.OrderByDescending(e => e.StartDate)),
            resumes,
            interviews,
            evaluation is null ? null : ToEvaluationDto(evaluation));
    }

    public async Task<(Stream content, string contentType, string fileName)> DownloadCandidateResumeAsync(
        Guid userId, Guid applicationId, Guid resumeId, CancellationToken cancellationToken = default)
    {
        var hm = await GetHiringManagerAsync(userId, cancellationToken);
        var orgId = RequireOrganization(hm);

        // Authorize via the review relationship: the resume must belong to the candidate of an
        // application within the manager's organization.
        var application = await _uow.Applications.Query()
            .Include(a => a.Job)
            .Include(a => a.Candidate).ThenInclude(c => c.Resumes)
            .FirstOrDefaultAsync(a => a.Id == applicationId && a.Job.OrganizationId == orgId, cancellationToken)
            ?? throw new NotFoundException("Application", applicationId);

        var resume = application.Candidate.Resumes.FirstOrDefault(r => r.Id == resumeId)
            ?? throw new NotFoundException(nameof(Resume), resumeId);

        var stream = await _storage.GetAsync(resume.FilePath, cancellationToken)
            ?? throw new NotFoundException("Resume file", resumeId);

        return (stream, resume.ContentType, resume.FileName);
    }

    public async Task<EvaluationDto> SubmitEvaluationAsync(Guid userId, SubmitEvaluationRequest request, CancellationToken cancellationToken = default)
    {
        var hm = await GetHiringManagerAsync(userId, cancellationToken);
        var orgId = RequireOrganization(hm);

        var application = await _uow.Applications.Query().AsTracking()
            .Include(a => a.Job)
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId && a.Job.OrganizationId == orgId, cancellationToken)
            ?? throw new NotFoundException("Application", request.ApplicationId);

        var overall = (int)Math.Round((request.TechnicalScore + request.CommunicationScore + request.CultureFitScore) / 3.0);

        var evaluation = await _uow.Repository<CandidateEvaluation>().Query().AsTracking()
            .FirstOrDefaultAsync(e => e.ApplicationId == application.Id && e.HiringManagerId == hm.Id, cancellationToken);

        if (evaluation is null)
        {
            evaluation = new CandidateEvaluation { ApplicationId = application.Id, HiringManagerId = hm.Id };
            await _uow.Repository<CandidateEvaluation>().AddAsync(evaluation, cancellationToken);
        }

        evaluation.TechnicalScore = request.TechnicalScore;
        evaluation.CommunicationScore = request.CommunicationScore;
        evaluation.CultureFitScore = request.CultureFitScore;
        evaluation.OverallScore = overall;
        evaluation.Comments = request.Comments;
        evaluation.EvaluatedAt = DateTime.UtcNow;

        // Moving an application into evaluation marks it as interviewed if it was earlier in the pipeline.
        if (application.Status is ApplicationStatus.Shortlisted or ApplicationStatus.InterviewScheduled)
        {
            application.Status = ApplicationStatus.Interviewed;
            application.StatusChangedAt = DateTime.UtcNow;
        }

        await _uow.SaveChangesAsync(cancellationToken);
        return ToEvaluationDto(evaluation, hm);
    }

    public async Task<InterviewFeedbackDto> SubmitInterviewFeedbackAsync(Guid userId, SubmitInterviewFeedbackRequest request, CancellationToken cancellationToken = default)
    {
        var hm = await GetHiringManagerAsync(userId, cancellationToken);
        var orgId = RequireOrganization(hm);

        var interview = await _uow.Repository<InterviewSchedule>().Query().AsTracking()
            .Include(i => i.Application).ThenInclude(a => a.Job)
            .FirstOrDefaultAsync(i => i.Id == request.InterviewScheduleId && i.Application.Job.OrganizationId == orgId, cancellationToken)
            ?? throw new NotFoundException("Interview", request.InterviewScheduleId);

        var feedback = await _uow.Repository<InterviewFeedback>().Query().AsTracking()
            .FirstOrDefaultAsync(f => f.InterviewScheduleId == interview.Id && f.InterviewerUserId == userId, cancellationToken);

        if (feedback is null)
        {
            feedback = new InterviewFeedback { InterviewScheduleId = interview.Id, InterviewerUserId = userId };
            await _uow.Repository<InterviewFeedback>().AddAsync(feedback, cancellationToken);
        }

        feedback.Rating = request.Rating;
        feedback.Strengths = request.Strengths;
        feedback.Weaknesses = request.Weaknesses;
        feedback.Comments = request.Comments;
        feedback.Recommendation = request.Recommendation;
        feedback.SubmittedAt = DateTime.UtcNow;

        if (interview.Status == InterviewStatus.Scheduled)
        {
            interview.Status = InterviewStatus.Completed;
        }

        await _uow.SaveChangesAsync(cancellationToken);

        return new InterviewFeedbackDto(feedback.Id, feedback.InterviewScheduleId, feedback.Rating,
            feedback.Strengths, feedback.Weaknesses, feedback.Comments, feedback.Recommendation,
            hm.User.FirstName + " " + hm.User.LastName, feedback.SubmittedAt);
    }

    public Task<EvaluationDto> ApproveHiringAsync(Guid userId, Guid applicationId, HiringDecisionRequest request, CancellationToken cancellationToken = default)
        => DecideAsync(userId, applicationId, request, approve: true, cancellationToken);

    public Task<EvaluationDto> RejectHiringAsync(Guid userId, Guid applicationId, HiringDecisionRequest request, CancellationToken cancellationToken = default)
        => DecideAsync(userId, applicationId, request, approve: false, cancellationToken);

    private async Task<EvaluationDto> DecideAsync(Guid userId, Guid applicationId, HiringDecisionRequest request, bool approve, CancellationToken cancellationToken)
    {
        var hm = await GetHiringManagerAsync(userId, cancellationToken);
        var orgId = RequireOrganization(hm);

        var application = await _uow.Applications.Query().AsTracking()
            .Include(a => a.Job).ThenInclude(j => j.Recruiter)
            .Include(a => a.Candidate)
            .FirstOrDefaultAsync(a => a.Id == applicationId && a.Job.OrganizationId == orgId, cancellationToken)
            ?? throw new NotFoundException("Application", applicationId);

        if (application.Status is ApplicationStatus.Hired or ApplicationStatus.Rejected or ApplicationStatus.Withdrawn)
        {
            throw new ConflictException($"A decision has already been made for this application ('{application.Status}').");
        }

        var evaluation = await _uow.Repository<CandidateEvaluation>().Query().AsTracking()
            .FirstOrDefaultAsync(e => e.ApplicationId == application.Id && e.HiringManagerId == hm.Id, cancellationToken)
            ?? throw new ConflictException("Submit an evaluation before making a hiring decision.");

        evaluation.Decision = approve ? EvaluationDecision.Approved : EvaluationDecision.Rejected;
        if (!string.IsNullOrWhiteSpace(request.Comments))
        {
            evaluation.Comments = request.Comments;
        }
        evaluation.EvaluatedAt = DateTime.UtcNow;

        application.Status = approve ? ApplicationStatus.Hired : ApplicationStatus.Rejected;
        application.StatusChangedAt = DateTime.UtcNow;

        await _uow.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync("HiringDecision", nameof(JobApplication), application.Id.ToString(),
            $"Hiring manager {(approve ? "approved (Hired)" : "rejected")} application {application.Id} for job \"{application.Job.Title}\".",
            userId, cancellationToken: cancellationToken);

        // Notify the candidate and the owning recruiter.
        var verb = approve ? "hired" : "not selected";
        await _notifications.NotifyAsync(application.Candidate.UserId,
            approve ? "You've been hired!" : "Application update",
            $"A hiring decision has been made for \"{application.Job.Title}\": you have been {verb}.",
            NotificationType.Application, $"/candidate/applications/{application.Id}", cancellationToken);

        if (application.Job.Recruiter is not null)
        {
            await _notifications.NotifyAsync(application.Job.Recruiter.UserId,
                "Hiring decision recorded",
                $"A candidate for \"{application.Job.Title}\" was {verb} by the hiring manager.",
                NotificationType.Application, $"/recruiter/jobs/{application.JobId}/pipeline", cancellationToken);
        }

        return ToEvaluationDto(evaluation, hm);
    }

    private static InterviewFeedbackDto ToFeedbackDto(InterviewFeedback f) => new(
        f.Id, f.InterviewScheduleId, f.Rating, f.Strengths, f.Weaknesses, f.Comments, f.Recommendation,
        f.InterviewerUser is null ? "Interviewer" : f.InterviewerUser.FirstName + " " + f.InterviewerUser.LastName,
        f.SubmittedAt);

    private static EvaluationDto ToEvaluationDto(CandidateEvaluation e) => new(
        e.Id, e.ApplicationId, e.TechnicalScore, e.CommunicationScore, e.CultureFitScore, e.OverallScore,
        e.Comments, e.Decision,
        e.HiringManager?.User is null ? "Hiring Manager" : e.HiringManager.User.FirstName + " " + e.HiringManager.User.LastName,
        e.EvaluatedAt);

    private static EvaluationDto ToEvaluationDto(CandidateEvaluation e, HiringManager hm) => new(
        e.Id, e.ApplicationId, e.TechnicalScore, e.CommunicationScore, e.CultureFitScore, e.OverallScore,
        e.Comments, e.Decision, hm.User.FirstName + " " + hm.User.LastName, e.EvaluatedAt);

    private async Task<HiringManager> GetHiringManagerAsync(Guid userId, CancellationToken cancellationToken)
        => await _uow.Repository<HiringManager>().Query()
               .Include(h => h.User)
               .FirstOrDefaultAsync(h => h.UserId == userId, cancellationToken)
           ?? throw new ForbiddenException("No hiring-manager profile is associated with this account.");

    private static Guid RequireOrganization(HiringManager hm)
        => hm.OrganizationId ?? throw new ValidationException("Organization", "Your hiring-manager account is not linked to an organization.");
}
