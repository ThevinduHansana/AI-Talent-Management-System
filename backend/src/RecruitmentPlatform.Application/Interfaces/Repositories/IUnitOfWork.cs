using RecruitmentPlatform.Domain.Common;
using RecruitmentPlatform.Domain.Entities;

namespace RecruitmentPlatform.Application.Interfaces.Repositories;

/// <summary>
/// Coordinates work across repositories within a single transaction boundary. Exposes
/// specialized repositories where custom queries are needed and a generic repository otherwise.
/// </summary>
public interface IUnitOfWork
{
    IUserRepository Users { get; }
    ICandidateRepository Candidates { get; }
    IJobRepository Jobs { get; }
    IApplicationRepository Applications { get; }
    ISkillRepository Skills { get; }
    IRefreshTokenRepository RefreshTokens { get; }
    IInterviewRepository Interviews { get; }

    /// <summary>Generic repository for entity types without a specialized interface.</summary>
    IGenericRepository<T> Repository<T>() where T : BaseEntity;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
