using RecruitmentPlatform.Application.Common.Models;
using RecruitmentPlatform.Application.DTOs.Admin;

namespace RecruitmentPlatform.Application.Interfaces.Services;

/// <summary>Platform-wide analytics for the administrator dashboard.</summary>
public interface IAnalyticsService
{
    Task<AnalyticsOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default);
}

/// <summary>Administrator user management: listing, provisioning and updating accounts.</summary>
public interface IAdminUserService
{
    Task<PagedResult<AdminUserDto>> GetUsersAsync(AdminUserQuery query, CancellationToken cancellationToken = default);
    Task<AdminUserDto> GetUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<AdminUserDto> CreateUserAsync(AdminCreateUserRequest request, CancellationToken cancellationToken = default);
    Task<AdminUserDto> UpdateUserAsync(Guid userId, AdminUpdateUserRequest request, Guid actingAdminId, CancellationToken cancellationToken = default);
}

/// <summary>Administrator management of organizations and their departments.</summary>
public interface IAdminOrganizationService
{
    Task<IReadOnlyList<AdminOrganizationDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<AdminOrganizationDto> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AdminOrganizationDto> CreateAsync(SaveOrganizationRequest request, CancellationToken cancellationToken = default);
    Task<AdminOrganizationDto> UpdateAsync(Guid id, SaveOrganizationRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AdminDepartmentDto> AddDepartmentAsync(Guid organizationId, SaveDepartmentRequest request, CancellationToken cancellationToken = default);
    Task RemoveDepartmentAsync(Guid organizationId, Guid departmentId, CancellationToken cancellationToken = default);
}

/// <summary>Administrator RBAC read + role-permission assignment.</summary>
public interface IAdminRoleService
{
    Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync(CancellationToken cancellationToken = default);
    Task<RoleDto> UpdateRolePermissionsAsync(Guid roleId, UpdateRolePermissionsRequest request, CancellationToken cancellationToken = default);
}

/// <summary>Administrator audit-log viewing.</summary>
public interface IAuditLogService
{
    Task<PagedResult<AuditLogDto>> GetLogsAsync(AuditLogQuery query, CancellationToken cancellationToken = default);
}
