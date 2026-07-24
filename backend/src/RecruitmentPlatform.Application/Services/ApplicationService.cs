using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using RecruitmentPlatform.Application.Common.Exceptions;
using RecruitmentPlatform.Application.Common.Models;
using RecruitmentPlatform.Application.DTOs.Applications;
using RecruitmentPlatform.Application.Interfaces.Repositories;
using RecruitmentPlatform.Application.Interfaces.Services;
using RecruitmentPlatform.Domain.Entities;
using RecruitmentPlatform.Domain.Enums;

namespace RecruitmentPlatform.Application.Services;

/// <summary>
/// Candidate-facing job application and saved-job workflow. Enforces ownership and the
/// business rules around applying, withdrawing and bookmarking jobs.
/// </summary>
public class ApplicationService : IApplicationService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly INotificationService _notifications;
    private readonly IAuditService _audit;

    public ApplicationService(IUnitOfWork uow, IMapper mapper, INotificationService notifications, IAuditService audit)
    {
        _uow = uow;
        _mapper = mapper;
        _notifications = notifications;
        _audit = audit;
    }

    public async Task<ApplicationDto> ApplyAsync(Guid userId, ApplyToJobRequest request, CancellationToken cancellationToken = default)
    {
        var candidate = await GetCandidateAsync(userId, cancellationToken);

        var job = await _uow.Jobs.GetByIdAsync(request.JobId, cancellationToken)
            ?? throw new NotFoundException(nameof(Job), request.JobId);

        if (job.Status != JobStatus.Open)
        {
            throw new ConflictException("This job is no longer accepting applications.");
        }

        // There can only ever be one row per (candidate, job) — the table has a unique index on
        // that pair. So a prior application must be reactivated, not duplicated.
        var existing = await _uow.Applications.Query().AsTracking()
            .FirstOrDefaultAsync(a => a.CandidateId == candidate.Id && a.JobId == job.Id, cancellationToken);

        if (existing is not null && existing.Status != ApplicationStatus.Withdrawn)
        {
            // An active (or already-decided) application blocks re-applying; only a withdrawn one
            // may be resurrected.
            throw new ConflictException("You have already applied to this job.");
        }

        if (request.ResumeId.HasValue)
        {
            var ownsResume = await _uow.Repository<Resume>()
                .AnyAsync(r => r.Id == request.ResumeId.Value && r.CandidateId == candidate.Id, cancellationToken);
            if (!ownsResume)
            {
                throw new ValidationException("ResumeId", "The selected resume was not found on your profile.");
            }
        }

        JobApplication application;
        if (existing is not null)
        {
            // Re-applying after a withdrawal: reset the existing row to a fresh application rather
            // than insert a duplicate (which the unique index would reject). Clear the previous
            // cycle's review artefacts so the recruiter sees a clean application.
            existing.Status = ApplicationStatus.Applied;
            existing.ResumeId = request.ResumeId;
            existing.CoverLetter = request.CoverLetter;
            existing.AppliedAt = DateTime.UtcNow;
            existing.StatusChangedAt = DateTime.UtcNow;
            existing.RecruiterNotes = null;
            existing.MatchScore = null;
            existing.RankScore = null;
            _uow.Applications.Update(existing);
            application = existing;
        }
        else
        {
            application = new JobApplication
            {
                JobId = job.Id,
                CandidateId = candidate.Id,
                ResumeId = request.ResumeId,
                CoverLetter = request.CoverLetter,
                Status = ApplicationStatus.Applied,
                AppliedAt = DateTime.UtcNow
            };
            await _uow.Applications.AddAsync(application, cancellationToken);
        }

        await _uow.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(
            existing is not null ? "ApplicationReapplied" : "ApplicationSubmitted",
            nameof(JobApplication), application.Id.ToString(),
            $"Candidate applied to job \"{job.Title}\" ({job.Id}).", userId, cancellationToken: cancellationToken);

        await _notifications.NotifyAsync(userId, "Application submitted",
            $"Your application for \"{job.Title}\" has been received.", NotificationType.Application,
            $"/candidate/applications/{application.Id}", cancellationToken);

        return await GetApplicationDtoAsync(application.Id, cancellationToken);
    }

    public async Task<PagedResult<ApplicationDto>> GetMyApplicationsAsync(Guid userId, ApplicationQuery query, CancellationToken cancellationToken = default)
    {
        var candidate = await GetCandidateAsync(userId, cancellationToken);

        var q = _uow.Applications.Query().Where(a => a.CandidateId == candidate.Id);
        if (query.Status.HasValue) q = q.Where(a => a.Status == query.Status.Value);

        q = q.OrderByDescending(a => a.AppliedAt);

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ProjectTo<ApplicationDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return new PagedResult<ApplicationDto>(items, total, query.Page, query.PageSize);
    }

    public async Task<ApplicationDto> GetApplicationAsync(Guid userId, Guid applicationId, CancellationToken cancellationToken = default)
    {
        var candidate = await GetCandidateAsync(userId, cancellationToken);
        var dto = await _uow.Applications.Query()
            .Where(a => a.Id == applicationId && a.CandidateId == candidate.Id)
            .ProjectTo<ApplicationDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);

        return dto ?? throw new NotFoundException("Application", applicationId);
    }

    public async Task WithdrawAsync(Guid userId, Guid applicationId, CancellationToken cancellationToken = default)
    {
        var candidate = await GetCandidateAsync(userId, cancellationToken);
        var application = await _uow.Applications
            .FirstOrDefaultAsync(a => a.Id == applicationId && a.CandidateId == candidate.Id, cancellationToken)
            ?? throw new NotFoundException("Application", applicationId);

        if (application.Status is ApplicationStatus.Hired or ApplicationStatus.Rejected or ApplicationStatus.Withdrawn)
        {
            throw new ConflictException($"An application in '{application.Status}' state cannot be withdrawn.");
        }

        application.Status = ApplicationStatus.Withdrawn;
        application.StatusChangedAt = DateTime.UtcNow;
        _uow.Applications.Update(application);
        await _uow.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync("ApplicationWithdrawn", nameof(JobApplication), application.Id.ToString(),
            $"Candidate withdrew application {application.Id} (job {application.JobId}).",
            userId, cancellationToken: cancellationToken);
    }

    public async Task<SavedJobDto> SaveJobAsync(Guid userId, Guid jobId, CancellationToken cancellationToken = default)
    {
        var candidate = await GetCandidateAsync(userId, cancellationToken);

        if (!await _uow.Jobs.AnyAsync(j => j.Id == jobId, cancellationToken))
        {
            throw new NotFoundException(nameof(Job), jobId);
        }

        var existing = await _uow.Repository<SavedJob>()
            .FirstOrDefaultAsync(s => s.CandidateId == candidate.Id && s.JobId == jobId, cancellationToken);
        if (existing is not null)
        {
            return await GetSavedJobDtoAsync(existing.Id, cancellationToken);
        }

        var saved = new SavedJob { CandidateId = candidate.Id, JobId = jobId };
        await _uow.Repository<SavedJob>().AddAsync(saved, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return await GetSavedJobDtoAsync(saved.Id, cancellationToken);
    }

    public async Task UnsaveJobAsync(Guid userId, Guid jobId, CancellationToken cancellationToken = default)
    {
        var candidate = await GetCandidateAsync(userId, cancellationToken);
        var saved = await _uow.Repository<SavedJob>()
            .FirstOrDefaultAsync(s => s.CandidateId == candidate.Id && s.JobId == jobId, cancellationToken);
        if (saved is null) return;

        _uow.Repository<SavedJob>().Remove(saved);
        await _uow.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SavedJobDto>> GetSavedJobsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var candidate = await GetCandidateAsync(userId, cancellationToken);
        return await _uow.Repository<SavedJob>().Query()
            .Where(s => s.CandidateId == candidate.Id)
            .OrderByDescending(s => s.SavedAt)
            .ProjectTo<SavedJobDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
    }

    private async Task<Candidate> GetCandidateAsync(Guid userId, CancellationToken cancellationToken)
        => await _uow.Candidates.GetByUserIdAsync(userId, cancellationToken)
           ?? throw new NotFoundException("Candidate profile", userId);

    private async Task<ApplicationDto> GetApplicationDtoAsync(Guid applicationId, CancellationToken cancellationToken)
        => await _uow.Applications.Query()
               .Where(a => a.Id == applicationId)
               .ProjectTo<ApplicationDto>(_mapper.ConfigurationProvider)
               .FirstAsync(cancellationToken);

    private async Task<SavedJobDto> GetSavedJobDtoAsync(Guid savedJobId, CancellationToken cancellationToken)
        => await _uow.Repository<SavedJob>().Query()
               .Where(s => s.Id == savedJobId)
               .ProjectTo<SavedJobDto>(_mapper.ConfigurationProvider)
               .FirstAsync(cancellationToken);
}
