using AutoMapper;
using RecruitmentPlatform.Application.DTOs.Applications;
using RecruitmentPlatform.Application.DTOs.Auth;
using RecruitmentPlatform.Application.DTOs.Candidates;
using RecruitmentPlatform.Application.DTOs.Jobs;
using RecruitmentPlatform.Application.DTOs.Recruiter;
using RecruitmentPlatform.Domain.Entities;

namespace RecruitmentPlatform.Application.Mappings;

/// <summary>
/// Central AutoMapper configuration mapping domain entities to their DTOs. Constructor
/// parameters that require navigation flattening are mapped explicitly for reliability.
/// </summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, AuthUserDto>()
            .ForCtorParam(nameof(AuthUserDto.Roles),
                opt => opt.MapFrom(u => u.UserRoles.Select(ur => ur.Role.Name).ToList()));

        // Candidate leaf DTOs
        CreateMap<CandidateSkill, CandidateSkillDto>()
            .ForCtorParam("SkillName", opt => opt.MapFrom(cs => cs.Skill.Name))
            .ForCtorParam("Category", opt => opt.MapFrom(cs => cs.Skill.Category));

        CreateMap<Education, EducationDto>();
        CreateMap<Experience, ExperienceDto>();
        CreateMap<Resume, ResumeDto>();
        CreateMap<Certificate, CertificateDto>();

        // Jobs
        CreateMap<JobSkill, JobSkillDto>()
            .ForCtorParam("SkillName", opt => opt.MapFrom(js => js.Skill.Name));

        CreateMap<Job, JobListItemDto>()
            .ForCtorParam("OrganizationName", opt => opt.MapFrom(j => j.Organization.Name))
            .ForCtorParam("DepartmentName", opt => opt.MapFrom(j => j.Department != null ? j.Department.Name : null))
            .ForCtorParam("ApplicationCount", opt => opt.MapFrom(j => j.Applications.Count));

        CreateMap<Job, JobDetailDto>()
            .ForCtorParam("OrganizationName", opt => opt.MapFrom(j => j.Organization.Name))
            .ForCtorParam("DepartmentName", opt => opt.MapFrom(j => j.Department != null ? j.Department.Name : null))
            .ForCtorParam("Skills", opt => opt.MapFrom(j => j.JobSkills));

        // Applications
        CreateMap<JobApplication, ApplicationDto>()
            .ForCtorParam("JobTitle", opt => opt.MapFrom(a => a.Job.Title))
            .ForCtorParam("OrganizationName", opt => opt.MapFrom(a => a.Job.Organization.Name))
            .ForCtorParam("Location", opt => opt.MapFrom(a => a.Job.Location))
            .ForCtorParam("CandidateName", opt => opt.MapFrom(a => a.Candidate.User.FirstName + " " + a.Candidate.User.LastName))
            .ForCtorParam("RecruiterUserId", opt => opt.MapFrom(a => a.Job.Recruiter.UserId))
            .ForCtorParam("RecruiterName", opt => opt.MapFrom(a => a.Job.Recruiter.User.FirstName + " " + a.Job.Recruiter.User.LastName));

        CreateMap<SavedJob, SavedJobDto>()
            .ForCtorParam("JobTitle", opt => opt.MapFrom(s => s.Job.Title))
            .ForCtorParam("OrganizationName", opt => opt.MapFrom(s => s.Job.Organization.Name))
            .ForCtorParam("Location", opt => opt.MapFrom(s => s.Job.Location));

        // Recruiter pipeline view
        CreateMap<JobApplication, RecruiterApplicationDto>()
            .ForCtorParam("JobTitle", opt => opt.MapFrom(a => a.Job.Title))
            .ForCtorParam("CandidateUserId", opt => opt.MapFrom(a => a.Candidate.UserId))
            .ForCtorParam("CandidateName", opt => opt.MapFrom(a => a.Candidate.User.FirstName + " " + a.Candidate.User.LastName))
            .ForCtorParam("CandidateEmail", opt => opt.MapFrom(a => a.Candidate.User.Email))
            .ForCtorParam("Headline", opt => opt.MapFrom(a => a.Candidate.Headline))
            .ForCtorParam("Resumes", opt => opt.MapFrom(a => a.Candidate.Resumes.OrderByDescending(r => r.IsPrimary).ThenByDescending(r => r.UploadedAt)));

        CreateMap<InterviewSchedule, InterviewDto>()
            .ForCtorParam("CandidateName", opt => opt.MapFrom(i => i.Application.Candidate.User.FirstName + " " + i.Application.Candidate.User.LastName))
            .ForCtorParam("JobTitle", opt => opt.MapFrom(i => i.Application.Job.Title));
    }
}
