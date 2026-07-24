using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RecruitmentPlatform.Application.Common.Exceptions;
using RecruitmentPlatform.Application.DTOs.Account;
using RecruitmentPlatform.Application.Interfaces.Infrastructure;
using RecruitmentPlatform.Application.Interfaces.Repositories;
using RecruitmentPlatform.Application.Interfaces.Services;
using RecruitmentPlatform.Domain.Entities;

namespace RecruitmentPlatform.Application.Services;

/// <summary>
/// Implements the self-service data-privacy operations. Erasure anonymises rather than hard-deletes
/// the user, so records other parties rely on (a recruiter's view of past applicants, audit trail)
/// keep their referential integrity while the personal data itself is removed.
/// </summary>
public class AccountService : IAccountService
{
    private readonly IUnitOfWork _uow;
    private readonly IFileStorageService _storage;
    private readonly IAuditService _audit;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<AccountService> _logger;

    public AccountService(
        IUnitOfWork uow,
        IFileStorageService storage,
        IAuditService audit,
        IPasswordHasher passwordHasher,
        ILogger<AccountService> logger)
    {
        _uow = uow;
        _storage = storage;
        _audit = audit;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<PersonalDataExport> ExportPersonalDataAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _uow.Repository<User>().Query()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), userId);

        var account = new AccountInfo(user.Id, user.FirstName, user.LastName, user.Email, user.PhoneNumber,
            user.IsEmailConfirmed, user.CreatedAt, user.LastLoginAt);

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();

        var candidate = await _uow.Repository<Candidate>().Query()
            .Include(c => c.CandidateSkills).ThenInclude(cs => cs.Skill)
            .Include(c => c.Educations)
            .Include(c => c.Experiences)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        CandidateProfileExport? profile = null;
        var applications = new List<ApplicationExport>();
        var interviews = new List<InterviewExport>();
        var savedJobs = new List<SavedJobExport>();
        var resumes = new List<ResumeExport>();

        if (candidate is not null)
        {
            profile = new CandidateProfileExport(
                candidate.Headline, candidate.Summary, candidate.Location, candidate.CurrentPosition,
                candidate.YearsOfExperience, candidate.LinkedInUrl, candidate.PortfolioUrl,
                candidate.CandidateSkills.Select(cs => cs.Skill.Name).ToList(),
                candidate.Educations.Select(e => $"{e.Degree}, {e.Institution}").ToList(),
                candidate.Experiences.Select(e => $"{e.Title} at {e.Company}").ToList());

            applications = await _uow.Applications.Query()
                .Where(a => a.CandidateId == candidate.Id)
                .Select(a => new ApplicationExport(a.Id, a.Job.Title, a.Job.Organization!.Name,
                    a.Status.ToString(), a.AppliedAt, a.CoverLetter))
                .ToListAsync(cancellationToken);

            interviews = await _uow.Interviews.Query()
                .Where(i => i.Application.CandidateId == candidate.Id)
                .Select(i => new InterviewExport(i.Id, i.Title, i.ScheduledAt, i.DurationMinutes,
                    i.Status.ToString(), i.Location, i.MeetingLink))
                .ToListAsync(cancellationToken);

            savedJobs = await _uow.Repository<SavedJob>().Query()
                .Where(s => s.CandidateId == candidate.Id)
                .Select(s => new SavedJobExport(s.JobId, s.Job.Title, s.SavedAt))
                .ToListAsync(cancellationToken);

            resumes = await _uow.Repository<Resume>().Query()
                .Where(r => r.CandidateId == candidate.Id)
                .Select(r => new ResumeExport(r.Id, r.FileName, r.FileSize, r.IsPrimary, r.UploadedAt))
                .ToListAsync(cancellationToken);
        }

        var notifications = await _uow.Repository<Notification>().Query()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationExport(n.Title, n.Message, n.Type.ToString(), n.IsRead, n.CreatedAt))
            .ToListAsync(cancellationToken);

        var messages = await _uow.Repository<Message>().Query()
            .Where(m => m.SenderUserId == userId || m.RecipientUserId == userId)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new MessageExport(
                m.SenderUserId == userId ? "sent" : "received",
                m.Body, m.IsRead, m.CreatedAt))
            .ToListAsync(cancellationToken);

        await _audit.LogAsync("PersonalDataExported", nameof(User), userId.ToString(),
            "User exported their personal data.", userId, cancellationToken: cancellationToken);

        return new PersonalDataExport(DateTime.UtcNow, account, roles, profile, applications, interviews,
            savedJobs, resumes, notifications, messages);
    }

    public async Task<AccountDeletionResult> DeleteAccountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _uow.Repository<User>().Query().AsTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), userId);

        var resumesDeleted = 0;

        var candidate = await _uow.Repository<Candidate>().Query().AsTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        if (candidate is not null)
        {
            // Resume documents are the most sensitive PII. Detach them from any applications that
            // reference them (the FK is optional) before removing the files and rows, so erasure
            // never trips the foreign key.
            var resumes = await _uow.Repository<Resume>().Query().AsTracking()
                .Where(r => r.CandidateId == candidate.Id)
                .ToListAsync(cancellationToken);

            if (resumes.Count > 0)
            {
                var resumeIds = resumes.Select(r => r.Id).ToHashSet();
                var referencing = await _uow.Applications.Query().AsTracking()
                    .Where(a => a.ResumeId != null && resumeIds.Contains(a.ResumeId.Value))
                    .ToListAsync(cancellationToken);
                foreach (var app in referencing)
                {
                    app.ResumeId = null;
                }

                foreach (var resume in resumes)
                {
                    try
                    {
                        await _storage.DeleteAsync(resume.FilePath, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        // A missing/failed file delete must not block erasure of the database record.
                        _logger.LogWarning(ex, "Failed to delete resume file {Path} during account erasure.", resume.FilePath);
                    }
                    _uow.Repository<Resume>().Remove(resume);
                    resumesDeleted++;
                }
            }

            // De-identify free-text profile fields that can carry personal information.
            candidate.Headline = null;
            candidate.Summary = null;
            candidate.Location = null;
            candidate.LinkedInUrl = null;
            candidate.PortfolioUrl = null;
            candidate.CurrentPosition = null;
            candidate.DateOfBirth = null;
        }

        // Revoke every session.
        var tokens = await _uow.Repository<RefreshToken>().Query().AsTracking()
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var token in tokens)
        {
            token.RevokedAt = DateTime.UtcNow;
        }

        // Anonymise the identity. A tombstone email keeps the unique-email constraint satisfied
        // while removing the real address, and a random unusable password hash prevents login.
        var tombstone = $"deleted+{user.Id:N}@deleted.local";
        user.FirstName = "Deleted";
        user.LastName = "User";
        user.Email = tombstone;
        user.NormalizedEmail = tombstone.ToUpperInvariant();
        user.PhoneNumber = null;
        user.ProfilePictureUrl = null;
        user.PasswordHash = _passwordHasher.Hash(Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"));
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiresAt = null;
        user.IsActive = false;

        await _uow.SaveChangesAsync(cancellationToken);

        // Audit last, so the erasure itself is recorded (the audit row references the now-anonymised
        // user id, which is retained precisely for this accountability purpose).
        await _audit.LogAsync("AccountErased", nameof(User), userId.ToString(),
            $"User account erased on request. {resumesDeleted} resume document(s) deleted.",
            userId, cancellationToken: cancellationToken);

        _logger.LogInformation("Account {UserId} erased on user request ({Resumes} resumes removed).", userId, resumesDeleted);

        return new AccountDeletionResult(
            "Your account has been deleted. Your personal data has been erased and you have been signed out.",
            resumesDeleted);
    }
}
