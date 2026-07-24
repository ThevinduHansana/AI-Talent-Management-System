using RecruitmentPlatform.Application.DTOs.Common;
using RecruitmentPlatform.Domain.Enums;

namespace RecruitmentPlatform.Application.DTOs.Applications;

public record ApplyToJobRequest(Guid JobId, Guid? ResumeId, string? CoverLetter);

public record ApplicationDto(
    Guid Id,
    Guid JobId,
    string JobTitle,
    string OrganizationName,
    string? Location,
    Guid CandidateId,
    string CandidateName,
    Guid RecruiterUserId,
    string RecruiterName,
    ApplicationStatus Status,
    double? MatchScore,
    double? RankScore,
    string? CoverLetter,
    DateTime AppliedAt,
    DateTime? StatusChangedAt);

public record SavedJobDto(Guid Id, Guid JobId, string JobTitle, string OrganizationName, string? Location, DateTime SavedAt);

/// <summary>Filter parameters for a candidate's application list.</summary>
public class ApplicationQuery : PaginationQuery
{
    public ApplicationStatus? Status { get; set; }
}
