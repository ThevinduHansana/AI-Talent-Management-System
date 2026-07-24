using Microsoft.EntityFrameworkCore;
using RecruitmentPlatform.Application.Interfaces.Repositories;
using RecruitmentPlatform.Domain.Entities;
using RecruitmentPlatform.Domain.Enums;
using RecruitmentPlatform.Infrastructure.Data;

namespace RecruitmentPlatform.Infrastructure.Repositories;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext context) : base(context) { }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToUpperInvariant();
        return await Set.FirstOrDefaultAsync(u => u.NormalizedEmail == normalized, cancellationToken);
    }

    public async Task<User?> GetByEmailWithRolesAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToUpperInvariant();
        return await Set
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalized, cancellationToken);
    }

    public async Task<User?> GetWithRolesAsync(Guid userId, CancellationToken cancellationToken = default)
        => await Set
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToUpperInvariant();
        return await Set.AnyAsync(u => u.NormalizedEmail == normalized, cancellationToken);
    }
}

public class CandidateRepository : GenericRepository<Candidate>, ICandidateRepository
{
    public CandidateRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Candidate?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => await Set.FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

    public async Task<Candidate?> GetFullProfileByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => await Set
            .Include(c => c.User)
            .Include(c => c.CandidateSkills).ThenInclude(cs => cs.Skill)
            .Include(c => c.Educations)
            .Include(c => c.Experiences)
            .Include(c => c.Resumes)
            .Include(c => c.Certificates)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
}

public class JobRepository : GenericRepository<Job>, IJobRepository
{
    public JobRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Job?> GetWithDetailsAsync(Guid jobId, CancellationToken cancellationToken = default)
        => await Set
            .Include(j => j.Organization)
            .Include(j => j.Department)
            .Include(j => j.JobSkills).ThenInclude(js => js.Skill)
            .FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);
}

public class ApplicationRepository : GenericRepository<JobApplication>, IApplicationRepository
{
    public ApplicationRepository(ApplicationDbContext context) : base(context) { }

    public async Task<JobApplication?> GetWithDetailsAsync(Guid applicationId, CancellationToken cancellationToken = default)
        => await Set
            .Include(a => a.Job).ThenInclude(j => j.Organization)
            .Include(a => a.Candidate).ThenInclude(c => c.User)
            .FirstOrDefaultAsync(a => a.Id == applicationId, cancellationToken);

    public async Task<bool> HasAppliedAsync(Guid candidateId, Guid jobId, CancellationToken cancellationToken = default)
        => await Set.AnyAsync(a => a.CandidateId == candidateId && a.JobId == jobId, cancellationToken);
}

public class SkillRepository : GenericRepository<Skill>, ISkillRepository
{
    public SkillRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Skill?> GetByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken = default)
        => await Set.FirstOrDefaultAsync(s => s.NormalizedName == normalizedName, cancellationToken);
}

public class RefreshTokenRepository : GenericRepository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(ApplicationDbContext context) : base(context) { }

    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
        => await Set.FirstOrDefaultAsync(t => t.Token == token, cancellationToken);
}

public class InterviewRepository : GenericRepository<InterviewSchedule>, IInterviewRepository
{
    public InterviewRepository(ApplicationDbContext context) : base(context) { }

    public async Task<InterviewSchedule?> GetWithDetailsAsync(Guid interviewId, CancellationToken cancellationToken = default)
        => await Set
            .Include(i => i.Application).ThenInclude(a => a.Job).ThenInclude(j => j.Organization)
            .Include(i => i.Application).ThenInclude(a => a.Candidate).ThenInclude(c => c.User)
            .Include(i => i.InterviewerUser)
            .FirstOrDefaultAsync(i => i.Id == interviewId, cancellationToken);

    public async Task<bool> HasCandidateOverlapAsync(Guid candidateId, DateTime startUtc, DateTime endUtc,
        Guid? excludeInterviewId = null, CancellationToken cancellationToken = default)
    {
        var query = Set.Where(i =>
            i.Application.CandidateId == candidateId
            // Only still-active interviews clash; a Cancelled (or Completed) one frees its slot.
            && i.Status == InterviewStatus.Scheduled
            // Half-open intervals: an interview ending exactly when the next starts is not a clash.
            && i.ScheduledAt < endUtc
            && startUtc < i.ScheduledAt.AddMinutes(i.DurationMinutes));

        if (excludeInterviewId is { } excluded)
        {
            query = query.Where(i => i.Id != excluded);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
