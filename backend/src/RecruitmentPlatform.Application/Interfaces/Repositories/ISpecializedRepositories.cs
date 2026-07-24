using RecruitmentPlatform.Domain.Entities;

namespace RecruitmentPlatform.Application.Interfaces.Repositories;

public interface IUserRepository : IGenericRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Loads a user with roles eagerly for token generation and authorization.</summary>
    Task<User?> GetByEmailWithRolesAsync(string email, CancellationToken cancellationToken = default);

    Task<User?> GetWithRolesAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
}

public interface ICandidateRepository : IGenericRepository<Candidate>
{
    Task<Candidate?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Loads the full candidate profile graph (skills, education, experience, resumes).</summary>
    Task<Candidate?> GetFullProfileByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}

public interface IJobRepository : IGenericRepository<Job>
{
    Task<Job?> GetWithDetailsAsync(Guid jobId, CancellationToken cancellationToken = default);
}

public interface IApplicationRepository : IGenericRepository<JobApplication>
{
    Task<JobApplication?> GetWithDetailsAsync(Guid applicationId, CancellationToken cancellationToken = default);

    Task<bool> HasAppliedAsync(Guid candidateId, Guid jobId, CancellationToken cancellationToken = default);
}

public interface ISkillRepository : IGenericRepository<Skill>
{
    Task<Skill?> GetByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken = default);
}

public interface IInterviewRepository : IGenericRepository<InterviewSchedule>
{
    /// <summary>Loads an interview with the graph needed to build an invitation (job, org, candidate).</summary>
    Task<InterviewSchedule?> GetWithDetailsAsync(Guid interviewId, CancellationToken cancellationToken = default);

    /// <summary>
    /// True when the candidate already has a still-scheduled interview overlapping the given
    /// window. Scoped to the candidate (not the recruiter) because a candidate genuinely cannot be
    /// in two interviews at once, whereas a recruiter routinely has several running in parallel,
    /// conducted by different interviewers. Cancelled interviews never count.
    /// <paramref name="excludeInterviewId"/> lets a reschedule ignore the row being moved.
    /// </summary>
    Task<bool> HasCandidateOverlapAsync(Guid candidateId, DateTime startUtc, DateTime endUtc,
        Guid? excludeInterviewId = null, CancellationToken cancellationToken = default);
}

public interface IRefreshTokenRepository : IGenericRepository<RefreshToken>
{
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
}
