using System.Linq.Expressions;
using RecruitmentPlatform.Domain.Common;

namespace RecruitmentPlatform.Application.Interfaces.Repositories;

/// <summary>
/// Generic persistence-ignorant repository for an aggregate/entity type. Implementations live
/// in the infrastructure layer over EF Core.
/// </summary>
public interface IGenericRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> ListAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> ListAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);

    Task AddAsync(T entity, CancellationToken cancellationToken = default);

    void Update(T entity);

    void Remove(T entity);

    /// <summary>Returns a composable query for advanced scenarios (paging, projection, includes).</summary>
    IQueryable<T> Query();
}
