using RecruitmentPlatform.Application.DTOs.Candidates;
using RecruitmentPlatform.Application.DTOs.Common;
using RecruitmentPlatform.Domain.Enums;

namespace RecruitmentPlatform.Application.DTOs.Recruiter;

// ----- Job management -----

public record JobSkillInput(string SkillName, string? Category, bool IsRequired, ProficiencyLevel MinimumProficiency, int Weight);

public record SaveJobRequest(
    string Title,
    string Description,
    string? Responsibilities,
    string? Requirements,
    EmploymentType EmploymentType,
    ExperienceLevel ExperienceLevel,
    string? Location,
    bool IsRemote,
    decimal? SalaryMin,
    decimal? SalaryMax,
    string Currency,
    int Vacancies,
    Guid? DepartmentId,
    DateTime? ClosingDate,
    JobStatus Status,
    IReadOnlyList<JobSkillInput> Skills);

public record RecruiterJobDto(
    Guid Id,
    string Title,
    JobStatus Status,
    EmploymentType EmploymentType,
    ExperienceLevel ExperienceLevel,
    string? Location,
    bool IsRemote,
    int Vacancies,
    DateTime? PostedAt,
    DateTime? ClosingDate,
    int ApplicationCount,
    int ShortlistedCount,
    DateTime CreatedAt);

public class RecruiterJobQuery : PaginationQuery
{
    public JobStatus? Status { get; set; }
}

// ----- Application pipeline -----

public record RecruiterApplicationDto(
    Guid Id,
    Guid JobId,
    string JobTitle,
    Guid CandidateId,
    Guid CandidateUserId,
    string CandidateName,
    string CandidateEmail,
    string? Headline,
    ApplicationStatus Status,
    double? MatchScore,
    double? RankScore,
    string? CoverLetter,
    string? RecruiterNotes,
    Guid? ResumeId,
    IReadOnlyList<ResumeDto> Resumes,
    DateTime AppliedAt);

public class RecruiterApplicationQuery : PaginationQuery
{
    public ApplicationStatus? Status { get; set; }
}

public record UpdateApplicationStatusRequest(ApplicationStatus Status, string? Notes);

// ----- Interviews -----

public record ScheduleInterviewRequest(
    Guid ApplicationId,
    string Title,
    DateTime ScheduledAt,
    int DurationMinutes,
    InterviewMode Mode,
    string? Location,
    string? MeetingLink,
    Guid? InterviewerUserId,
    string? Notes);

public record InterviewDto(
    Guid Id,
    Guid ApplicationId,
    string CandidateName,
    string JobTitle,
    string Title,
    DateTime ScheduledAt,
    int DurationMinutes,
    InterviewMode Mode,
    string? Location,
    string? MeetingLink,
    InterviewStatus Status,
    string? Notes);
