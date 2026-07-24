using Microsoft.EntityFrameworkCore;
using RecruitmentPlatform.Application.Common.Exceptions;
using RecruitmentPlatform.Application.Common.Models;
using RecruitmentPlatform.Application.DTOs.Admin;
using RecruitmentPlatform.Application.Interfaces.Infrastructure;
using RecruitmentPlatform.Application.Interfaces.Repositories;
using RecruitmentPlatform.Application.Interfaces.Services;
using RecruitmentPlatform.Domain.Common;
using RecruitmentPlatform.Domain.Entities;

namespace RecruitmentPlatform.Application.Services;

/// <summary>
/// Administrator user management: listing/searching accounts, provisioning privileged users and
/// updating account status, roles and organizational assignment.
/// </summary>
public class AdminUserService : IAdminUserService
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditService _audit;

    public AdminUserService(IUnitOfWork uow, IPasswordHasher passwordHasher, IAuditService audit)
    {
        _uow = uow;
        _passwordHasher = passwordHasher;
        _audit = audit;
    }

    public async Task<PagedResult<AdminUserDto>> GetUsersAsync(AdminUserQuery query, CancellationToken cancellationToken = default)
    {
        var q = _uow.Users.Query()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Include(u => u.Organization)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            q = q.Where(u => u.FirstName.ToLower().Contains(term)
                             || u.LastName.ToLower().Contains(term)
                             || u.Email.ToLower().Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(query.Role))
        {
            var normalized = query.Role.Trim().ToUpperInvariant();
            q = q.Where(u => u.UserRoles.Any(ur => ur.Role.NormalizedName == normalized));
        }
        if (query.IsActive.HasValue)
        {
            q = q.Where(u => u.IsActive == query.IsActive.Value);
        }

        var total = await q.CountAsync(cancellationToken);
        var users = await q
            .OrderByDescending(u => u.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminUserDto>(users.Select(ToDto).ToList(), total, query.Page, query.PageSize);
    }

    public async Task<AdminUserDto> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await LoadUserAsync(userId, cancellationToken) ?? throw new NotFoundException(nameof(User), userId);
        return ToDto(user);
    }

    public async Task<AdminUserDto> CreateUserAsync(AdminCreateUserRequest request, CancellationToken cancellationToken = default)
    {
        if (!RoleNames.All.Contains(request.Role))
        {
            throw new ValidationException("Role", "Unknown role.");
        }
        if (await _uow.Users.EmailExistsAsync(request.Email, cancellationToken))
        {
            throw new ConflictException("An account with this email already exists.");
        }

        var role = await _uow.Repository<Role>()
            .FirstOrDefaultAsync(r => r.NormalizedName == request.Role.ToUpperInvariant(), cancellationToken)
            ?? throw new NotFoundException("Role", request.Role);

        var user = new User
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email.Trim(),
            NormalizedEmail = request.Email.Trim().ToUpperInvariant(),
            PasswordHash = _passwordHasher.Hash(request.Password),
            PhoneNumber = request.PhoneNumber,
            IsActive = true,
            IsEmailConfirmed = true,
            OrganizationId = request.OrganizationId,
            DepartmentId = request.DepartmentId,
        };
        user.UserRoles.Add(new UserRole { Role = role });
        AttachProfileForRole(user, request.Role, request.OrganizationId, request.DepartmentId);

        await _uow.Users.AddAsync(user, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync("AdminCreatedUser", nameof(User), user.Id.ToString(),
            $"{user.Email} ({request.Role})", cancellationToken: cancellationToken);

        return ToDto((await LoadUserAsync(user.Id, cancellationToken))!);
    }

    public async Task<AdminUserDto> UpdateUserAsync(Guid userId, AdminUpdateUserRequest request, Guid actingAdminId, CancellationToken cancellationToken = default)
    {
        var user = await LoadUserAsync(userId, cancellationToken, tracked: true)
            ?? throw new NotFoundException(nameof(User), userId);

        var requestedRoles = request.Roles.Where(r => RoleNames.All.Contains(r)).Distinct().ToList();
        if (requestedRoles.Count == 0)
        {
            throw new ValidationException("Roles", "A user must have at least one valid role.");
        }

        // Guard: an admin cannot lock themselves out.
        if (userId == actingAdminId)
        {
            if (!request.IsActive)
            {
                throw new ValidationException("IsActive", "You cannot deactivate your own account.");
            }
            if (!requestedRoles.Contains(RoleNames.Administrator))
            {
                throw new ValidationException("Roles", "You cannot remove your own administrator role.");
            }
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.PhoneNumber = request.PhoneNumber;
        user.IsActive = request.IsActive;
        user.OrganizationId = request.OrganizationId;
        user.DepartmentId = request.DepartmentId;

        await ReconcileRolesAsync(user, requestedRoles, cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync("AdminUpdatedUser", nameof(User), user.Id.ToString(),
            $"roles=[{string.Join(',', requestedRoles)}] active={request.IsActive}", actingAdminId,
            cancellationToken: cancellationToken);

        return ToDto((await LoadUserAsync(user.Id, cancellationToken))!);
    }

    private async Task ReconcileRolesAsync(User user, IReadOnlyList<string> requestedRoles, CancellationToken cancellationToken)
    {
        var allRoles = await _uow.Repository<Role>().Query().ToListAsync(cancellationToken);
        var requestedNormalized = requestedRoles.Select(r => r.ToUpperInvariant()).ToHashSet();

        // Remove roles no longer requested.
        foreach (var ur in user.UserRoles.ToList())
        {
            if (!requestedNormalized.Contains(ur.Role.NormalizedName))
            {
                user.UserRoles.Remove(ur);
            }
        }

        // Add newly requested roles and ensure the matching profile exists.
        var currentNormalized = user.UserRoles.Select(ur => ur.Role.NormalizedName).ToHashSet();
        foreach (var roleName in requestedRoles)
        {
            var normalized = roleName.ToUpperInvariant();
            if (currentNormalized.Contains(normalized)) continue;

            var role = allRoles.First(r => r.NormalizedName == normalized);
            user.UserRoles.Add(new UserRole { RoleId = role.Id });
            AttachProfileForRole(user, roleName, user.OrganizationId, user.DepartmentId);
        }
    }

    private static void AttachProfileForRole(User user, string role, Guid? organizationId, Guid? departmentId)
    {
        switch (role)
        {
            case RoleNames.Candidate when user.Candidate is null:
                user.Candidate = new Candidate();
                break;
            case RoleNames.Recruiter when user.Recruiter is null:
                user.Recruiter = new Recruiter { OrganizationId = organizationId };
                break;
            case RoleNames.HiringManager when user.HiringManager is null:
                user.HiringManager = new HiringManager { OrganizationId = organizationId, DepartmentId = departmentId };
                break;
        }
    }

    private async Task<User?> LoadUserAsync(Guid userId, CancellationToken cancellationToken, bool tracked = false)
    {
        var query = _uow.Users.Query()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Include(u => u.Organization)
            .Include(u => u.Candidate)
            .Include(u => u.Recruiter)
            .Include(u => u.HiringManager)
            .AsQueryable();
        if (tracked) query = query.AsTracking();
        return await query.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
    }

    private static AdminUserDto ToDto(User u) => new(
        u.Id, u.FirstName, u.LastName, u.Email, u.PhoneNumber, u.IsActive, u.IsEmailConfirmed,
        u.LastLoginAt, u.CreatedAt,
        u.UserRoles.Select(ur => ur.Role.Name).OrderBy(n => n).ToList(),
        u.OrganizationId, u.Organization?.Name);
}
