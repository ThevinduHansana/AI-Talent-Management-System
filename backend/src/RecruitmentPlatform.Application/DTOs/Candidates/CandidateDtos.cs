using RecruitmentPlatform.Domain.Enums;

namespace RecruitmentPlatform.Application.DTOs.Candidates;

public record CandidateProfileDto(
    Guid Id,
    Guid UserId,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    string? ProfilePictureUrl,
    string? Headline,
    string? Summary,
    string? Location,
    DateTime? DateOfBirth,
    Gender Gender,
    string? LinkedInUrl,
    string? PortfolioUrl,
    string? CurrentPosition,
    int YearsOfExperience,
    decimal? ExpectedSalary,
    string? PreferredCurrency,
    AvailabilityStatus AvailabilityStatus,
    IReadOnlyList<CandidateSkillDto> Skills,
    IReadOnlyList<EducationDto> Education,
    IReadOnlyList<ExperienceDto> Experience,
    IReadOnlyList<ResumeDto> Resumes,
    IReadOnlyList<CertificateDto> Certificates);

public record UpdateCandidateProfileRequest(
    string? Headline,
    string? Summary,
    string? Location,
    DateTime? DateOfBirth,
    Gender Gender,
    string? LinkedInUrl,
    string? PortfolioUrl,
    string? CurrentPosition,
    int YearsOfExperience,
    decimal? ExpectedSalary,
    string? PreferredCurrency,
    AvailabilityStatus AvailabilityStatus,
    string? PhoneNumber);

public record CandidateSkillDto(Guid Id, Guid SkillId, string SkillName, string Category, ProficiencyLevel ProficiencyLevel, int YearsOfExperience);

public record AddCandidateSkillRequest(string SkillName, string? Category, ProficiencyLevel ProficiencyLevel, int YearsOfExperience);

public record EducationDto(Guid Id, string Institution, string Degree, string? FieldOfStudy, DateTime StartDate, DateTime? EndDate, bool IsCurrent, string? Grade, string? Description);

public record SaveEducationRequest(string Institution, string Degree, string? FieldOfStudy, DateTime StartDate, DateTime? EndDate, bool IsCurrent, string? Grade, string? Description);

public record ExperienceDto(Guid Id, string Company, string Title, string? Location, EmploymentType EmploymentType, DateTime StartDate, DateTime? EndDate, bool IsCurrent, string? Description);

public record SaveExperienceRequest(string Company, string Title, string? Location, EmploymentType EmploymentType, DateTime StartDate, DateTime? EndDate, bool IsCurrent, string? Description);

public record ResumeDto(Guid Id, string FileName, string ContentType, long FileSize, bool IsPrimary, DateTime UploadedAt);

public record CertificateDto(Guid Id, string Name, string? IssuingOrganization, DateTime? IssueDate, DateTime? ExpiryDate, string? CredentialId, string? CredentialUrl, string? FileName);

public record SaveCertificateRequest(string Name, string? IssuingOrganization, DateTime? IssueDate, DateTime? ExpiryDate, string? CredentialId, string? CredentialUrl);
