using RecruitmentPlatform.Application.Interfaces.Repositories;
using RecruitmentPlatform.Application.Interfaces.Services;
using RecruitmentPlatform.Domain.Entities;

namespace RecruitmentPlatform.Application.Services;

/// <summary>
/// Persists audit-log entries. Writes are committed independently so an audit record survives
/// even when it accompanies a read-only or already-committed operation.
/// </summary>
public class AuditService : IAuditService
{
    private readonly IUnitOfWork _uow;

    public AuditService(IUnitOfWork uow) => _uow = uow;

    public async Task LogAsync(string action, string? entityName = null, string? entityId = null,
        string? details = null, Guid? userId = null, string? ipAddress = null, string? userAgent = null,
        int? statusCode = null, CancellationToken cancellationToken = default)
    {
        var entry = new AuditLog
        {
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Details = details,
            UserId = userId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            StatusCode = statusCode
        };

        await _uow.Repository<AuditLog>().AddAsync(entry, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
