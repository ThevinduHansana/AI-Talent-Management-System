using System.Collections.Concurrent;
using RecruitmentPlatform.Application.Interfaces.Repositories;
using RecruitmentPlatform.Domain.Common;
using RecruitmentPlatform.Infrastructure.Data;

namespace RecruitmentPlatform.Infrastructure.Repositories;

/// <summary>
/// Wraps the DbContext as a unit of work. Specialized repositories are exposed directly; generic
/// repositories are created on demand and cached per context instance.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly ConcurrentDictionary<Type, object> _genericRepositories = new();

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
        Users = new UserRepository(context);
        Candidates = new CandidateRepository(context);
        Jobs = new JobRepository(context);
        Applications = new ApplicationRepository(context);
        Skills = new SkillRepository(context);
        RefreshTokens = new RefreshTokenRepository(context);
        Interviews = new InterviewRepository(context);
    }

    public IUserRepository Users { get; }
    public ICandidateRepository Candidates { get; }
    public IJobRepository Jobs { get; }
    public IApplicationRepository Applications { get; }
    public ISkillRepository Skills { get; }
    public IRefreshTokenRepository RefreshTokens { get; }
    public IInterviewRepository Interviews { get; }

    public IGenericRepository<T> Repository<T>() where T : BaseEntity
        => (IGenericRepository<T>)_genericRepositories.GetOrAdd(typeof(T), _ => new GenericRepository<T>(_context));

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
