using RecruitmentPlatform.Application.DTOs.Common;

namespace RecruitmentPlatform.Application.DTOs.Admin;

// ----- Analytics -----

public record StatusCountDto(string Status, int Count);
public record LabelValueDto(string Label, int Value);
public record TimeSeriesPointDto(string Period, int Value);
public record RecruiterPerformanceDto(string RecruiterName, int JobsPosted, int Applications, int Hires);

public record AnalyticsOverviewDto(
    int TotalUsers,
    int TotalCandidates,
    int TotalRecruiters,
    int TotalHiringManagers,
    int ActiveJobs,
    int TotalJobs,
    int TotalApplications,
    int TotalInterviews,
    int Hires,
    double HiringRate,
    IReadOnlyList<StatusCountDto> ApplicationsByStatus,
    IReadOnlyList<LabelValueDto> TopSkills,
    IReadOnlyList<LabelValueDto> DepartmentHiring,
    IReadOnlyList<TimeSeriesPointDto> MonthlyApplications,
    IReadOnlyList<TimeSeriesPointDto> MonthlyHires,
    IReadOnlyList<RecruiterPerformanceDto> RecruiterPerformance);

// ----- Users -----

public record AdminUserDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    bool IsActive,
    bool IsEmailConfirmed,
    DateTime? LastLoginAt,
    DateTime CreatedAt,
    IReadOnlyList<string> Roles,
    Guid? OrganizationId,
    string? OrganizationName);

public class AdminUserQuery : PaginationQuery
{
    public string? Role { get; set; }
    public bool? IsActive { get; set; }
}

public record AdminCreateUserRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string? PhoneNumber,
    string Role,
    Guid? OrganizationId,
    Guid? DepartmentId);

public record AdminUpdateUserRequest(
    string FirstName,
    string LastName,
    string? PhoneNumber,
    bool IsActive,
    IReadOnlyList<string> Roles,
    Guid? OrganizationId,
    Guid? DepartmentId);

// ----- Organizations & departments -----

public record AdminDepartmentDto(Guid Id, string Name, string? Description, int JobCount);

public record AdminOrganizationDto(
    Guid Id,
    string Name,
    string? Description,
    string? Industry,
    string? Website,
    string? Location,
    bool IsActive,
    int DepartmentCount,
    int JobCount,
    IReadOnlyList<AdminDepartmentDto> Departments);

public record SaveOrganizationRequest(string Name, string? Description, string? Industry, string? Website, string? Location, bool IsActive);
public record SaveDepartmentRequest(string Name, string? Description);

// ----- Roles & permissions -----

public record PermissionDto(Guid Id, string Name, string? Description, string Category);
public record RoleDto(Guid Id, string Name, string? Description, int UserCount, IReadOnlyList<PermissionDto> Permissions);
public record UpdateRolePermissionsRequest(IReadOnlyList<Guid> PermissionIds);

// ----- Audit logs -----

public record AuditLogDto(
    Guid Id,
    string Action,
    string? EntityName,
    string? EntityId,
    string? Details,
    string? UserEmail,
    string? IpAddress,
    int? StatusCode,
    DateTime CreatedAt);

public class AuditLogQuery : PaginationQuery
{
    public string? Action { get; set; }
}
