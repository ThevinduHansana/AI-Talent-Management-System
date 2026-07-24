using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using RecruitmentPlatform.Application.Common.Exceptions;
using RecruitmentPlatform.Application.Common.Models;
using RecruitmentPlatform.Application.DTOs.Recruiter;
using RecruitmentPlatform.Application.Interfaces.Infrastructure;
using RecruitmentPlatform.Application.Interfaces.Repositories;
using RecruitmentPlatform.Application.Interfaces.Services;
using RecruitmentPlatform.Domain.Entities;
using RecruitmentPlatform.Domain.Enums;

namespace RecruitmentPlatform.Application.Services;

/// <summary>
/// Recruiter-facing application pipeline: reviewing applications, changing status (with candidate
/// notifications), recording notes and ranking candidates via the matching service.
/// </summary>
public class RecruiterApplicationService : IRecruiterApplicationService
{
    // Statuses a recruiter is allowed to set directly.
    private static readonly HashSet<ApplicationStatus> RecruiterSettableStatuses = new()
    {
        ApplicationStatus.UnderReview, ApplicationStatus.Shortlisted, ApplicationStatus.Rejected,
        ApplicationStatus.Offered, ApplicationStatus.Interviewed,
    };

    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly IMatchingService _matching;
    private readonly INotificationService _notifications;
    private readonly IFileStorageService _storage;
    private readonly IAuditService _audit;

    public RecruiterApplicationService(IUnitOfWork uow, IMapper mapper, IMatchingService matching, INotificationService notifications, IFileStorageService storage, IAuditService audit)
    {
        _uow = uow;
        _mapper = mapper;
        _matching = matching;
        _notifications = notifications;
        _storage = storage;
        _audit = audit;
    }

    public async Task<PagedResult<RecruiterApplicationDto>> GetForJobAsync(Guid userId, Guid jobId, RecruiterApplicationQuery query, CancellationToken cancellationToken = default)
    {
        var recruiter = await GetRecruiterAsync(userId, cancellationToken);
        await EnsureJobOwnedAsync(jobId, recruiter.Id, cancellationToken);

        var q = _uow.Applications.Query().Where(a => a.JobId == jobId);
        if (query.Status.HasValue) q = q.Where(a => a.Status == query.Status.Value);

        // Highest match first, then most recent.
        q = q.OrderByDescending(a => a.RankScore ?? a.MatchScore ?? -1).ThenByDescending(a => a.AppliedAt);

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ProjectTo<RecruiterApplicationDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return new PagedResult<RecruiterApplicationDto>(items, total, query.Page, query.PageSize);
    }

    public async Task<RecruiterApplicationDto> GetAsync(Guid userId, Guid applicationId, CancellationToken cancellationToken = default)
    {
        var recruiter = await GetRecruiterAsync(userId, cancellationToken);
        var dto = await _uow.Applications.Query()
            .Where(a => a.Id == applicationId && a.Job.RecruiterId == recruiter.Id)
            .ProjectTo<RecruiterApplicationDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);

        return dto ?? throw new NotFoundException("Application", applicationId);
    }

    public async Task<RecruiterApplicationDto> UpdateStatusAsync(Guid userId, Guid applicationId, UpdateApplicationStatusRequest request, CancellationToken cancellationToken = default)
    {
        var recruiter = await GetRecruiterAsync(userId, cancellationToken);

        if (!RecruiterSettableStatuses.Contains(request.Status))
        {
            throw new ValidationException("Status", $"A recruiter cannot set an application to '{request.Status}'.");
        }

        var application = await _uow.Applications.Query().AsTracking()
            .Include(a => a.Job)
            .Include(a => a.Candidate)
            .FirstOrDefaultAsync(a => a.Id == applicationId && a.Job.RecruiterId == recruiter.Id, cancellationToken)
            ?? throw new NotFoundException("Application", applicationId);

        var previousStatus = application.Status;
        application.Status = request.Status;
        application.StatusChangedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            application.RecruiterNotes = request.Notes;
        }

        await _uow.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync("ApplicationStatusChanged", nameof(JobApplication), application.Id.ToString(),
            $"Recruiter changed status of application {application.Id} from {previousStatus} to {request.Status}.",
            userId, cancellationToken: cancellationToken);

        await _notifications.NotifyAsync(
            application.Candidate.UserId,
            "Application update",
            $"Your application for \"{application.Job.Title}\" is now {Humanize(request.Status)}.",
            NotificationType.Application,
            $"/candidate/applications/{application.Id}",
            cancellationToken);

        return await GetAsync(userId, applicationId, cancellationToken);
    }

    public async Task<(Stream content, string contentType, string fileName)> DownloadCandidateResumeAsync(
        Guid userId, Guid applicationId, Guid resumeId, CancellationToken cancellationToken = default)
    {
        var recruiter = await GetRecruiterAsync(userId, cancellationToken);

        // Authorize via ownership: the resume must belong to the candidate of an application on one of
        // the recruiter's jobs.
        var application = await _uow.Applications.Query()
            .Include(a => a.Candidate).ThenInclude(c => c.Resumes)
            .FirstOrDefaultAsync(a => a.Id == applicationId && a.Job.RecruiterId == recruiter.Id, cancellationToken)
            ?? throw new NotFoundException("Application", applicationId);

        var resume = application.Candidate.Resumes.FirstOrDefault(r => r.Id == resumeId)
            ?? throw new NotFoundException(nameof(Resume), resumeId);

        var stream = await _storage.GetAsync(resume.FilePath, cancellationToken)
            ?? throw new NotFoundException("Resume file", resumeId);

        return (stream, resume.ContentType, resume.FileName);
    }

    public async Task<IReadOnlyList<RecruiterApplicationDto>> RankAsync(Guid userId, Guid jobId, CancellationToken cancellationToken = default)
    {
        var recruiter = await GetRecruiterAsync(userId, cancellationToken);
        await EnsureJobOwnedAsync(jobId, recruiter.Id, cancellationToken);

        var applications = await _uow.Applications.Query().AsTracking()
            .Where(a => a.JobId == jobId)
            .ToListAsync(cancellationToken);

        foreach (var application in applications)
        {
            var score = await _matching.ScoreCandidateForJobAsync(application.CandidateId, jobId, cancellationToken);
            application.MatchScore = score;
            application.RankScore = score;
        }

        await _uow.SaveChangesAsync(cancellationToken);

        return await _uow.Applications.Query()
            .Where(a => a.JobId == jobId)
            .OrderByDescending(a => a.RankScore)
            .ProjectTo<RecruiterApplicationDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
    }

    private async Task EnsureJobOwnedAsync(Guid jobId, Guid recruiterId, CancellationToken cancellationToken)
    {
        var owned = await _uow.Jobs.AnyAsync(j => j.Id == jobId && j.RecruiterId == recruiterId, cancellationToken);
        if (!owned)
        {
            throw new NotFoundException(nameof(Job), jobId);
        }
    }

    private async Task<Recruiter> GetRecruiterAsync(Guid userId, CancellationToken cancellationToken)
        => await _uow.Repository<Recruiter>().FirstOrDefaultAsync(r => r.UserId == userId, cancellationToken)
           ?? throw new ForbiddenException("No recruiter profile is associated with this account.");

    private static string Humanize(ApplicationStatus status) =>
        System.Text.RegularExpressions.Regex.Replace(status.ToString(), "([a-z])([A-Z])", "$1 $2").ToLowerInvariant();
}
