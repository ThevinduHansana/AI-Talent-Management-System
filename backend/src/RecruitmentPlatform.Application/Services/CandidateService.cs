using AutoMapper;
using RecruitmentPlatform.Application.Common.Exceptions;
using RecruitmentPlatform.Application.DTOs.Candidates;
using RecruitmentPlatform.Application.Interfaces.Repositories;
using RecruitmentPlatform.Application.Interfaces.Services;
using RecruitmentPlatform.Domain.Entities;

namespace RecruitmentPlatform.Application.Services;

/// <summary>
/// Implements candidate self-service. Every operation resolves the candidate profile from the
/// authenticated user id, so a candidate can only ever read or mutate their own data.
/// </summary>
public class CandidateService : ICandidateService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public CandidateService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<CandidateProfileDto> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var candidate = await _uow.Candidates.GetFullProfileByUserIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("Candidate profile", userId);

        return BuildProfileDto(candidate);
    }

    public async Task<CandidateProfileDto> UpdateProfileAsync(Guid userId, UpdateCandidateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var candidate = await _uow.Candidates.GetFullProfileByUserIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("Candidate profile", userId);

        candidate.Headline = request.Headline;
        candidate.Summary = request.Summary;
        candidate.Location = request.Location;
        candidate.DateOfBirth = request.DateOfBirth is { } dob ? DateTime.SpecifyKind(dob, DateTimeKind.Utc) : null;
        candidate.Gender = request.Gender;
        candidate.LinkedInUrl = request.LinkedInUrl;
        candidate.PortfolioUrl = request.PortfolioUrl;
        candidate.CurrentPosition = request.CurrentPosition;
        candidate.YearsOfExperience = request.YearsOfExperience;
        candidate.ExpectedSalary = request.ExpectedSalary;
        candidate.PreferredCurrency = request.PreferredCurrency;
        candidate.AvailabilityStatus = request.AvailabilityStatus;
        candidate.User.PhoneNumber = request.PhoneNumber ?? candidate.User.PhoneNumber;

        _uow.Candidates.Update(candidate);
        await _uow.SaveChangesAsync(cancellationToken);

        return BuildProfileDto(candidate);
    }

    public async Task<CandidateSkillDto> AddSkillAsync(Guid userId, AddCandidateSkillRequest request, CancellationToken cancellationToken = default)
    {
        var candidate = await GetOwnedCandidateAsync(userId, cancellationToken);

        var normalized = request.SkillName.Trim().ToUpperInvariant();
        var skill = await _uow.Skills.GetByNormalizedNameAsync(normalized, cancellationToken);
        if (skill is null)
        {
            skill = new Skill
            {
                Name = request.SkillName.Trim(),
                NormalizedName = normalized,
                Category = string.IsNullOrWhiteSpace(request.Category) ? "General" : request.Category!.Trim()
            };
            await _uow.Skills.AddAsync(skill, cancellationToken);
        }

        var alreadyHas = await _uow.Repository<CandidateSkill>()
            .AnyAsync(cs => cs.CandidateId == candidate.Id && cs.SkillId == skill.Id, cancellationToken);
        if (alreadyHas)
        {
            throw new ConflictException("This skill is already on your profile.");
        }

        var candidateSkill = new CandidateSkill
        {
            CandidateId = candidate.Id,
            Skill = skill,
            ProficiencyLevel = request.ProficiencyLevel,
            YearsOfExperience = request.YearsOfExperience
        };
        await _uow.Repository<CandidateSkill>().AddAsync(candidateSkill, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return new CandidateSkillDto(candidateSkill.Id, skill.Id, skill.Name, skill.Category,
            candidateSkill.ProficiencyLevel, candidateSkill.YearsOfExperience);
    }

    public async Task RemoveSkillAsync(Guid userId, Guid candidateSkillId, CancellationToken cancellationToken = default)
    {
        var candidate = await GetOwnedCandidateAsync(userId, cancellationToken);
        var candidateSkill = await _uow.Repository<CandidateSkill>()
            .FirstOrDefaultAsync(cs => cs.Id == candidateSkillId && cs.CandidateId == candidate.Id, cancellationToken)
            ?? throw new NotFoundException("Skill", candidateSkillId);

        _uow.Repository<CandidateSkill>().Remove(candidateSkill);
        await _uow.SaveChangesAsync(cancellationToken);
    }

    public async Task<EducationDto> AddEducationAsync(Guid userId, SaveEducationRequest request, CancellationToken cancellationToken = default)
    {
        var candidate = await GetOwnedCandidateAsync(userId, cancellationToken);
        var education = new Education
        {
            CandidateId = candidate.Id,
            Institution = request.Institution,
            Degree = request.Degree,
            FieldOfStudy = request.FieldOfStudy,
            StartDate = ToUtc(request.StartDate),
            EndDate = request.EndDate is { } e ? ToUtc(e) : null,
            IsCurrent = request.IsCurrent,
            Grade = request.Grade,
            Description = request.Description
        };
        await _uow.Repository<Education>().AddAsync(education, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return _mapper.Map<EducationDto>(education);
    }

    public async Task<EducationDto> UpdateEducationAsync(Guid userId, Guid educationId, SaveEducationRequest request, CancellationToken cancellationToken = default)
    {
        var candidate = await GetOwnedCandidateAsync(userId, cancellationToken);
        var education = await _uow.Repository<Education>()
            .FirstOrDefaultAsync(x => x.Id == educationId && x.CandidateId == candidate.Id, cancellationToken)
            ?? throw new NotFoundException("Education", educationId);

        education.Institution = request.Institution;
        education.Degree = request.Degree;
        education.FieldOfStudy = request.FieldOfStudy;
        education.StartDate = ToUtc(request.StartDate);
        education.EndDate = request.EndDate is { } e ? ToUtc(e) : null;
        education.IsCurrent = request.IsCurrent;
        education.Grade = request.Grade;
        education.Description = request.Description;

        _uow.Repository<Education>().Update(education);
        await _uow.SaveChangesAsync(cancellationToken);
        return _mapper.Map<EducationDto>(education);
    }

    public async Task RemoveEducationAsync(Guid userId, Guid educationId, CancellationToken cancellationToken = default)
    {
        var candidate = await GetOwnedCandidateAsync(userId, cancellationToken);
        var education = await _uow.Repository<Education>()
            .FirstOrDefaultAsync(x => x.Id == educationId && x.CandidateId == candidate.Id, cancellationToken)
            ?? throw new NotFoundException("Education", educationId);
        _uow.Repository<Education>().Remove(education);
        await _uow.SaveChangesAsync(cancellationToken);
    }

    public async Task<ExperienceDto> AddExperienceAsync(Guid userId, SaveExperienceRequest request, CancellationToken cancellationToken = default)
    {
        var candidate = await GetOwnedCandidateAsync(userId, cancellationToken);
        var experience = new Experience
        {
            CandidateId = candidate.Id,
            Company = request.Company,
            Title = request.Title,
            Location = request.Location,
            EmploymentType = request.EmploymentType,
            StartDate = ToUtc(request.StartDate),
            EndDate = request.EndDate is { } e ? ToUtc(e) : null,
            IsCurrent = request.IsCurrent,
            Description = request.Description
        };
        await _uow.Repository<Experience>().AddAsync(experience, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return _mapper.Map<ExperienceDto>(experience);
    }

    public async Task<ExperienceDto> UpdateExperienceAsync(Guid userId, Guid experienceId, SaveExperienceRequest request, CancellationToken cancellationToken = default)
    {
        var candidate = await GetOwnedCandidateAsync(userId, cancellationToken);
        var experience = await _uow.Repository<Experience>()
            .FirstOrDefaultAsync(x => x.Id == experienceId && x.CandidateId == candidate.Id, cancellationToken)
            ?? throw new NotFoundException("Experience", experienceId);

        experience.Company = request.Company;
        experience.Title = request.Title;
        experience.Location = request.Location;
        experience.EmploymentType = request.EmploymentType;
        experience.StartDate = ToUtc(request.StartDate);
        experience.EndDate = request.EndDate is { } e ? ToUtc(e) : null;
        experience.IsCurrent = request.IsCurrent;
        experience.Description = request.Description;

        _uow.Repository<Experience>().Update(experience);
        await _uow.SaveChangesAsync(cancellationToken);
        return _mapper.Map<ExperienceDto>(experience);
    }

    public async Task RemoveExperienceAsync(Guid userId, Guid experienceId, CancellationToken cancellationToken = default)
    {
        var candidate = await GetOwnedCandidateAsync(userId, cancellationToken);
        var experience = await _uow.Repository<Experience>()
            .FirstOrDefaultAsync(x => x.Id == experienceId && x.CandidateId == candidate.Id, cancellationToken)
            ?? throw new NotFoundException("Experience", experienceId);
        _uow.Repository<Experience>().Remove(experience);
        await _uow.SaveChangesAsync(cancellationToken);
    }

    public async Task<CertificateDto> AddCertificateAsync(Guid userId, SaveCertificateRequest request, CancellationToken cancellationToken = default)
    {
        var candidate = await GetOwnedCandidateAsync(userId, cancellationToken);
        var certificate = new Certificate
        {
            CandidateId = candidate.Id,
            Name = request.Name,
            IssuingOrganization = request.IssuingOrganization,
            IssueDate = request.IssueDate is { } i ? ToUtc(i) : null,
            ExpiryDate = request.ExpiryDate is { } ex ? ToUtc(ex) : null,
            CredentialId = request.CredentialId,
            CredentialUrl = request.CredentialUrl
        };
        await _uow.Repository<Certificate>().AddAsync(certificate, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return _mapper.Map<CertificateDto>(certificate);
    }

    public async Task RemoveCertificateAsync(Guid userId, Guid certificateId, CancellationToken cancellationToken = default)
    {
        var candidate = await GetOwnedCandidateAsync(userId, cancellationToken);
        var certificate = await _uow.Repository<Certificate>()
            .FirstOrDefaultAsync(x => x.Id == certificateId && x.CandidateId == candidate.Id, cancellationToken)
            ?? throw new NotFoundException("Certificate", certificateId);
        _uow.Repository<Certificate>().Remove(certificate);
        await _uow.SaveChangesAsync(cancellationToken);
    }

    private async Task<Candidate> GetOwnedCandidateAsync(Guid userId, CancellationToken cancellationToken)
        => await _uow.Candidates.GetByUserIdAsync(userId, cancellationToken)
           ?? throw new NotFoundException("Candidate profile", userId);

    private static DateTime ToUtc(DateTime value)
        => value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private CandidateProfileDto BuildProfileDto(Candidate c)
    {
        return new CandidateProfileDto(
            c.Id, c.UserId, c.User.FirstName, c.User.LastName, c.User.Email, c.User.PhoneNumber,
            c.User.ProfilePictureUrl, c.Headline, c.Summary, c.Location, c.DateOfBirth, c.Gender,
            c.LinkedInUrl, c.PortfolioUrl, c.CurrentPosition, c.YearsOfExperience, c.ExpectedSalary,
            c.PreferredCurrency, c.AvailabilityStatus,
            _mapper.Map<List<CandidateSkillDto>>(c.CandidateSkills.OrderBy(s => s.Skill.Name)),
            _mapper.Map<List<EducationDto>>(c.Educations.OrderByDescending(e => e.StartDate)),
            _mapper.Map<List<ExperienceDto>>(c.Experiences.OrderByDescending(e => e.StartDate)),
            _mapper.Map<List<ResumeDto>>(c.Resumes.OrderByDescending(r => r.UploadedAt)),
            _mapper.Map<List<CertificateDto>>(c.Certificates.OrderByDescending(ct => ct.IssueDate)));
    }
}
