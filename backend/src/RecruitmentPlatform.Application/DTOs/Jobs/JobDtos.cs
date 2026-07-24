using RecruitmentPlatform.Application.DTOs.Common;
using RecruitmentPlatform.Domain.Enums;

namespace RecruitmentPlatform.Application.DTOs.Jobs;

public record JobSkillDto(Guid SkillId, string SkillName, bool IsRequired, ProficiencyLevel MinimumProficiency, int Weight);

public record JobListItemDto(
    Guid Id,
    string Title,
    string OrganizationName,
    string? DepartmentName,
    string? Location,
    bool IsRemote,
    EmploymentType EmploymentType,
    ExperienceLevel ExperienceLevel,
    decimal? SalaryMin,
    decimal? SalaryMax,
    string Currency,
    JobStatus Status,
    DateTime? PostedAt,
    int ApplicationCount);

public record JobDetailDto(
    Guid Id,
    string Title,
    string Description,
    string? Responsibilities,
    string? Requirements,
    string OrganizationName,
    string? DepartmentName,
    string? Location,
    bool IsRemote,
    EmploymentType EmploymentType,
    ExperienceLevel ExperienceLevel,
    decimal? SalaryMin,
    decimal? SalaryMax,
    string Currency,
    int Vacancies,
    JobStatus Status,
    DateTime? PostedAt,
    DateTime? ClosingDate,
    IReadOnlyList<JobSkillDto> Skills);

/// <summary>Search/filter parameters for the public job board.</summary>
public class JobQuery : PaginationQuery
{
    public string? Location { get; set; }

    public bool? IsRemote { get; set; }

    public EmploymentType? EmploymentType { get; set; }

    public ExperienceLevel? ExperienceLevel { get; set; }

    public decimal? MinSalary { get; set; }

    public Guid? OrganizationId { get; set; }
}
