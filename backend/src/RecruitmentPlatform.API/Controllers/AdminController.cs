using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecruitmentPlatform.Application.Common.Models;
using RecruitmentPlatform.Application.DTOs.Admin;
using RecruitmentPlatform.Application.Interfaces.Services;
using RecruitmentPlatform.Domain.Common;

namespace RecruitmentPlatform.API.Controllers;

/// <summary>Administrator analytics dashboard.</summary>
[Authorize(Roles = RoleNames.Administrator)]
[Route("api/admin/analytics")]
public class AnalyticsController : ApiControllerBase
{
    private readonly IAnalyticsService _service;
    public AnalyticsController(IAnalyticsService service) => _service = service;

    /// <summary>Returns platform-wide recruitment metrics for the dashboard.</summary>
    [HttpGet("overview")]
    [ProducesResponseType(typeof(AnalyticsOverviewDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AnalyticsOverviewDto>> Overview(CancellationToken cancellationToken)
        => Ok(await _service.GetOverviewAsync(cancellationToken));
}

/// <summary>Administrator user management.</summary>
[Authorize(Roles = RoleNames.Administrator)]
[Route("api/admin/users")]
public class AdminUsersController : ApiControllerBase
{
    private readonly IAdminUserService _service;
    public AdminUsersController(IAdminUserService service) => _service = service;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AdminUserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AdminUserDto>>> GetUsers([FromQuery] AdminUserQuery query, CancellationToken cancellationToken)
        => Ok(await _service.GetUsersAsync(query, cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AdminUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminUserDto>> GetUser(Guid id, CancellationToken cancellationToken)
        => Ok(await _service.GetUserAsync(id, cancellationToken));

    [HttpPost]
    [ProducesResponseType(typeof(AdminUserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminUserDto>> CreateUser(AdminCreateUserRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.CreateUserAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetUser), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(AdminUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdminUserDto>> UpdateUser(Guid id, AdminUpdateUserRequest request, CancellationToken cancellationToken)
        => Ok(await _service.UpdateUserAsync(id, request, CurrentUserId, cancellationToken));
}

/// <summary>Administrator organization and department management.</summary>
[Authorize(Roles = RoleNames.Administrator)]
[Route("api/admin/organizations")]
public class AdminOrganizationsController : ApiControllerBase
{
    private readonly IAdminOrganizationService _service;
    public AdminOrganizationsController(IAdminOrganizationService service) => _service = service;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AdminOrganizationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminOrganizationDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _service.GetAllAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AdminOrganizationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminOrganizationDto>> Get(Guid id, CancellationToken cancellationToken)
        => Ok(await _service.GetAsync(id, cancellationToken));

    [HttpPost]
    [ProducesResponseType(typeof(AdminOrganizationDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<AdminOrganizationDto>> Create(SaveOrganizationRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(AdminOrganizationDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminOrganizationDto>> Update(Guid id, SaveOrganizationRequest request, CancellationToken cancellationToken)
        => Ok(await _service.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/departments")]
    [ProducesResponseType(typeof(AdminDepartmentDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<AdminDepartmentDto>> AddDepartment(Guid id, SaveDepartmentRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.AddDepartmentAsync(id, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpDelete("{id:guid}/departments/{departmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveDepartment(Guid id, Guid departmentId, CancellationToken cancellationToken)
    {
        await _service.RemoveDepartmentAsync(id, departmentId, cancellationToken);
        return NoContent();
    }
}

/// <summary>Administrator roles &amp; permissions.</summary>
[Authorize(Roles = RoleNames.Administrator)]
[Route("api/admin/roles")]
public class AdminRolesController : ApiControllerBase
{
    private readonly IAdminRoleService _service;
    public AdminRolesController(IAdminRoleService service) => _service = service;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RoleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RoleDto>>> GetRoles(CancellationToken cancellationToken)
        => Ok(await _service.GetRolesAsync(cancellationToken));

    [HttpGet("permissions")]
    [ProducesResponseType(typeof(IReadOnlyList<PermissionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PermissionDto>>> GetPermissions(CancellationToken cancellationToken)
        => Ok(await _service.GetPermissionsAsync(cancellationToken));

    [HttpPut("{id:guid}/permissions")]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<RoleDto>> UpdatePermissions(Guid id, UpdateRolePermissionsRequest request, CancellationToken cancellationToken)
        => Ok(await _service.UpdateRolePermissionsAsync(id, request, cancellationToken));
}

/// <summary>Administrator audit-log viewer.</summary>
[Authorize(Roles = RoleNames.Administrator)]
[Route("api/admin/audit-logs")]
public class AdminAuditController : ApiControllerBase
{
    private readonly IAuditLogService _service;
    public AdminAuditController(IAuditLogService service) => _service = service;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AuditLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> GetLogs([FromQuery] AuditLogQuery query, CancellationToken cancellationToken)
        => Ok(await _service.GetLogsAsync(query, cancellationToken));
}
