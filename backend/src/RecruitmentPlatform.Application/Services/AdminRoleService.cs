using Microsoft.EntityFrameworkCore;
using RecruitmentPlatform.Application.Common.Exceptions;
using RecruitmentPlatform.Application.Common.Models;
using RecruitmentPlatform.Application.DTOs.Admin;
using RecruitmentPlatform.Application.Interfaces.Repositories;
using RecruitmentPlatform.Application.Interfaces.Services;
using RecruitmentPlatform.Domain.Entities;

namespace RecruitmentPlatform.Application.Services;

/// <summary>Administrator RBAC: viewing roles/permissions and assigning permissions to roles.</summary>
public class AdminRoleService : IAdminRoleService
{
    private readonly IUnitOfWork _uow;

    public AdminRoleService(IUnitOfWork uow) => _uow = uow;

    public async Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _uow.Repository<Role>().Query()
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .Include(r => r.UserRoles)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);

        return roles.Select(r => new RoleDto(
            r.Id, r.Name, r.Description, r.UserRoles.Count,
            r.RolePermissions.Select(rp => ToPermissionDto(rp.Permission)).OrderBy(p => p.Category).ThenBy(p => p.Name).ToList()))
            .ToList();
    }

    public async Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync(CancellationToken cancellationToken = default)
    {
        var permissions = await _uow.Repository<Permission>().Query()
            .OrderBy(p => p.Category).ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);
        return permissions.Select(ToPermissionDto).ToList();
    }

    public async Task<RoleDto> UpdateRolePermissionsAsync(Guid roleId, UpdateRolePermissionsRequest request, CancellationToken cancellationToken = default)
    {
        var role = await _uow.Repository<Role>().Query().AsTracking()
            .Include(r => r.RolePermissions)
            .Include(r => r.UserRoles)
            .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken)
            ?? throw new NotFoundException(nameof(Role), roleId);

        var requestedIds = request.PermissionIds.Distinct().ToHashSet();

        var validIds = (await _uow.Repository<Permission>().Query()
            .Where(p => requestedIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync(cancellationToken))
            .ToHashSet();

        // Remove permissions no longer granted.
        foreach (var rp in role.RolePermissions.ToList())
        {
            if (!validIds.Contains(rp.PermissionId))
            {
                role.RolePermissions.Remove(rp);
            }
        }

        // Add newly granted permissions.
        var current = role.RolePermissions.Select(rp => rp.PermissionId).ToHashSet();
        foreach (var id in validIds.Where(id => !current.Contains(id)))
        {
            role.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = id });
        }

        await _uow.SaveChangesAsync(cancellationToken);

        var refreshed = await _uow.Repository<Role>().Query()
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .Include(r => r.UserRoles)
            .FirstAsync(r => r.Id == roleId, cancellationToken);

        return new RoleDto(refreshed.Id, refreshed.Name, refreshed.Description, refreshed.UserRoles.Count,
            refreshed.RolePermissions.Select(rp => ToPermissionDto(rp.Permission)).OrderBy(p => p.Category).ThenBy(p => p.Name).ToList());
    }

    private static PermissionDto ToPermissionDto(Permission p) => new(p.Id, p.Name, p.Description, p.Category);
}

/// <summary>Administrator audit-log viewing with filtering and pagination.</summary>
public class AuditLogService : IAuditLogService
{
    private readonly IUnitOfWork _uow;

    public AuditLogService(IUnitOfWork uow) => _uow = uow;

    public async Task<PagedResult<AuditLogDto>> GetLogsAsync(AuditLogQuery query, CancellationToken cancellationToken = default)
    {
        var q = _uow.Repository<AuditLog>().Query().Include(a => a.User).AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            var term = query.Action.Trim().ToLower();
            q = q.Where(a => a.Action.ToLower().Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            q = q.Where(a => (a.Details != null && a.Details.ToLower().Contains(term))
                             || (a.EntityName != null && a.EntityName.ToLower().Contains(term)));
        }

        q = q.OrderByDescending(a => a.CreatedAt);

        var total = await q.CountAsync(cancellationToken);
        var logs = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var items = logs.Select(a => new AuditLogDto(
            a.Id, a.Action, a.EntityName, a.EntityId, a.Details, a.User?.Email, a.IpAddress, a.StatusCode, a.CreatedAt))
            .ToList();

        return new PagedResult<AuditLogDto>(items, total, query.Page, query.PageSize);
    }
}
