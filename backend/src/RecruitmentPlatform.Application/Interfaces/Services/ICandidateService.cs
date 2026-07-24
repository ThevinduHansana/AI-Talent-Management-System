using RecruitmentPlatform.Application.DTOs.Candidates;

namespace RecruitmentPlatform.Application.Interfaces.Services;

/// <summary>
/// Candidate self-service operations. All methods operate on the candidate profile owned by the
/// supplied user id, enforcing that a candidate can only manage their own data.
/// </summary>
public interface ICandidateService
{
    Task<CandidateProfileDto> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<CandidateProfileDto> UpdateProfileAsync(Guid userId, UpdateCandidateProfileRequest request, CancellationToken cancellationToken = default);

    // Skills
    Task<CandidateSkillDto> AddSkillAsync(Guid userId, AddCandidateSkillRequest request, CancellationToken cancellationToken = default);
    Task RemoveSkillAsync(Guid userId, Guid candidateSkillId, CancellationToken cancellationToken = default);

    // Education
    Task<EducationDto> AddEducationAsync(Guid userId, SaveEducationRequest request, CancellationToken cancellationToken = default);
    Task<EducationDto> UpdateEducationAsync(Guid userId, Guid educationId, SaveEducationRequest request, CancellationToken cancellationToken = default);
    Task RemoveEducationAsync(Guid userId, Guid educationId, CancellationToken cancellationToken = default);

    // Experience
    Task<ExperienceDto> AddExperienceAsync(Guid userId, SaveExperienceRequest request, CancellationToken cancellationToken = default);
    Task<ExperienceDto> UpdateExperienceAsync(Guid userId, Guid experienceId, SaveExperienceRequest request, CancellationToken cancellationToken = default);
    Task RemoveExperienceAsync(Guid userId, Guid experienceId, CancellationToken cancellationToken = default);

    // Certificates
    Task<CertificateDto> AddCertificateAsync(Guid userId, SaveCertificateRequest request, CancellationToken cancellationToken = default);
    Task RemoveCertificateAsync(Guid userId, Guid certificateId, CancellationToken cancellationToken = default);
}
