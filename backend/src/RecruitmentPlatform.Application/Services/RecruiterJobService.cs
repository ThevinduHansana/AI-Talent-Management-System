using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using RecruitmentPlatform.Application.Common.Exceptions;
using RecruitmentPlatform.Application.Common.Models;
using RecruitmentPlatform.Application.DTOs.Jobs;
using RecruitmentPlatform.Application.DTOs.Recruiter;
using RecruitmentPlatform.Application.Interfaces.Repositories;
using RecruitmentPlatform.Application.Interfaces.Services;
using RecruitmentPlatform.Domain.Entities;
using RecruitmentPlatform.Domain.Enums;

namespace RecruitmentPlatform.Application.Services;

/// <summary>
/// Recruiter job management scoped to the recruiter resolved from the authenticated user.
/// </summary>
public class RecruiterJobService : IRecruiterJobService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public RecruiterJobService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<PagedResult<RecruiterJobDto>> GetMyJobsAsync(Guid userId, RecruiterJobQuery query, CancellationToken cancellationToken = default)
    {
        var recruiter = await GetRecruiterAsync(userId, cancellationToken);

        var q = _uow.Jobs.Query().Where(j => j.RecruiterId == recruiter.Id);
        if (query.Status.HasValue) q = q.Where(j => j.Status == query.Status.Value);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            q = q.Where(j => j.Title.ToLower().Contains(term));
        }

        q = q.OrderByDescending(j => j.CreatedAt);

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(j => new RecruiterJobDto(
                j.Id, j.Title, j.Status, j.EmploymentType, j.ExperienceLevel, j.Location, j.IsRemote,
                j.Vacancies, j.PostedAt, j.ClosingDate,
                j.Applications.Count,
                j.Applications.Count(a => a.Status == ApplicationStatus.Shortlisted),
                j.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<RecruiterJobDto>(items, total, query.Page, query.PageSize);
    }

    public async Task<JobDetailDto> GetMyJobAsync(Guid userId, Guid jobId, CancellationToken cancellationToken = default)
    {
        var recruiter = await GetRecruiterAsync(userId, cancellationToken);
        var dto = await _uow.Jobs.Query()
            .Where(j => j.Id == jobId && j.RecruiterId == recruiter.Id)
            .ProjectTo<JobDetailDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);

        return dto ?? throw new NotFoundException(nameof(Job), jobId);
    }

    public async Task<RecruiterJobDto> CreateAsync(Guid userId, SaveJobRequest request, CancellationToken cancellationToken = default)
    {
        var recruiter = await GetRecruiterAsync(userId, cancellationToken);
        if (recruiter.OrganizationId is null)
        {
            throw new ValidationException("Organization", "Your recruiter account is not linked to an organization.");
        }

        await ValidateDepartmentAsync(request.DepartmentId, recruiter.OrganizationId.Value, cancellationToken);

        var job = new Job
        {
            RecruiterId = recruiter.Id,
            OrganizationId = recruiter.OrganizationId.Value,
        };
        ApplyRequest(job, request);
        job.JobSkills = await BuildJobSkillsAsync(request.Skills, cancellationToken);

        await _uow.Jobs.AddAsync(job, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return ToJobDto(job, 0, 0);
    }

    public async Task<RecruiterJobDto> UpdateAsync(Guid userId, Guid jobId, SaveJobRequest request, CancellationToken cancellationToken = default)
    {
        var recruiter = await GetRecruiterAsync(userId, cancellationToken);

        // Tracked query so skill replacement and updates persist.
        var job = await _uow.Jobs.Query().AsTracking()
            .Include(j => j.JobSkills)
            .Include(j => j.Applications)
            .FirstOrDefaultAsync(j => j.Id == jobId && j.RecruiterId == recruiter.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Job), jobId);

        await ValidateDepartmentAsync(request.DepartmentId, job.OrganizationId, cancellationToken);

        ApplyRequest(job, request);

        // Reconcile the skill set: update retained skills in place, remove dropped ones, add new
        // ones. This avoids delete+insert of the same (JobId, SkillId) which would break the
        // unique index, and avoids navigation-reassignment tracking conflicts.
        var desired = new List<(Skill Skill, JobSkillInput Input)>();
        foreach (var input in request.Skills ?? new List<JobSkillInput>())
        {
            if (string.IsNullOrWhiteSpace(input.SkillName)) continue;
            desired.Add((await ResolveSkillAsync(input, cancellationToken), input));
        }

        var desiredBySkillId = desired.ToDictionary(d => d.Skill.Id, d => d.Input);

        foreach (var existing in job.JobSkills.ToList())
        {
            if (desiredBySkillId.TryGetValue(existing.SkillId, out var input))
            {
                existing.IsRequired = input.IsRequired;
                existing.MinimumProficiency = input.MinimumProficiency;
                existing.Weight = Math.Clamp(input.Weight, 1, 10);
            }
            else
            {
                job.JobSkills.Remove(existing);
                _uow.Repository<JobSkill>().Remove(existing);
            }
        }

        var existingSkillIds = job.JobSkills.Select(js => js.SkillId).ToHashSet();
        foreach (var (skill, input) in desired.Where(d => !existingSkillIds.Contains(d.Skill.Id)))
        {
            job.JobSkills.Add(new JobSkill
            {
                Skill = skill,
                IsRequired = input.IsRequired,
                MinimumProficiency = input.MinimumProficiency,
                Weight = Math.Clamp(input.Weight, 1, 10),
            });
        }

        await _uow.SaveChangesAsync(cancellationToken);

        var shortlisted = job.Applications.Count(a => a.Status == ApplicationStatus.Shortlisted);
        return ToJobDto(job, job.Applications.Count, shortlisted);
    }

    public async Task DeleteAsync(Guid userId, Guid jobId, CancellationToken cancellationToken = default)
    {
        var recruiter = await GetRecruiterAsync(userId, cancellationToken);
        var job = await _uow.Jobs
            .FirstOrDefaultAsync(j => j.Id == jobId && j.RecruiterId == recruiter.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Job), jobId);

        _uow.Jobs.Remove(job);
        await _uow.SaveChangesAsync(cancellationToken);
    }

    private static void ApplyRequest(Job job, SaveJobRequest request)
    {
        job.Title = request.Title.Trim();
        job.Description = request.Description;
        job.Responsibilities = request.Responsibilities;
        job.Requirements = request.Requirements;
        job.EmploymentType = request.EmploymentType;
        job.ExperienceLevel = request.ExperienceLevel;
        job.Location = request.Location;
        job.IsRemote = request.IsRemote;
        job.SalaryMin = request.SalaryMin;
        job.SalaryMax = request.SalaryMax;
        job.Currency = string.IsNullOrWhiteSpace(request.Currency) ? "USD" : request.Currency.Trim();
        job.Vacancies = request.Vacancies < 1 ? 1 : request.Vacancies;
        job.DepartmentId = request.DepartmentId;
        job.ClosingDate = request.ClosingDate is { } d ? DateTime.SpecifyKind(d, DateTimeKind.Utc) : null;
        job.Status = request.Status;

        // Stamp the publish date the first time a job goes live.
        if (request.Status == JobStatus.Open && job.PostedAt is null)
        {
            job.PostedAt = DateTime.UtcNow;
        }
    }

    private async Task<List<JobSkill>> BuildJobSkillsAsync(IReadOnlyList<JobSkillInput> inputs, CancellationToken cancellationToken)
    {
        var result = new List<JobSkill>();
        var seen = new HashSet<Guid>();
        foreach (var input in inputs ?? new List<JobSkillInput>())
        {
            if (string.IsNullOrWhiteSpace(input.SkillName)) continue;

            var skill = await ResolveSkillAsync(input, cancellationToken);
            if (!seen.Add(skill.Id)) continue; // Skip duplicate skills in the same request.

            result.Add(new JobSkill
            {
                Skill = skill,
                IsRequired = input.IsRequired,
                MinimumProficiency = input.MinimumProficiency,
                Weight = Math.Clamp(input.Weight, 1, 10),
            });
        }
        return result;
    }

    /// <summary>Finds the skill by normalized name, creating it in the catalog if it does not exist.</summary>
    private async Task<Skill> ResolveSkillAsync(JobSkillInput input, CancellationToken cancellationToken)
    {
        var normalized = input.SkillName.Trim().ToUpperInvariant();
        var skill = await _uow.Skills.GetByNormalizedNameAsync(normalized, cancellationToken);
        if (skill is null)
        {
            skill = new Skill
            {
                Name = input.SkillName.Trim(),
                NormalizedName = normalized,
                Category = string.IsNullOrWhiteSpace(input.Category) ? "General" : input.Category!.Trim(),
            };
            await _uow.Skills.AddAsync(skill, cancellationToken);
        }
        return skill;
    }

    private async Task ValidateDepartmentAsync(Guid? departmentId, Guid organizationId, CancellationToken cancellationToken)
    {
        if (departmentId is null) return;
        var valid = await _uow.Repository<Department>()
            .AnyAsync(d => d.Id == departmentId.Value && d.OrganizationId == organizationId, cancellationToken);
        if (!valid)
        {
            throw new ValidationException("DepartmentId", "The selected department does not belong to your organization.");
        }
    }

    private async Task<Recruiter> GetRecruiterAsync(Guid userId, CancellationToken cancellationToken)
        => await _uow.Repository<Recruiter>().FirstOrDefaultAsync(r => r.UserId == userId, cancellationToken)
           ?? throw new ForbiddenException("No recruiter profile is associated with this account.");

    private static RecruiterJobDto ToJobDto(Job job, int applicationCount, int shortlistedCount)
        => new(job.Id, job.Title, job.Status, job.EmploymentType, job.ExperienceLevel, job.Location,
            job.IsRemote, job.Vacancies, job.PostedAt, job.ClosingDate, applicationCount, shortlistedCount, job.CreatedAt);
}
