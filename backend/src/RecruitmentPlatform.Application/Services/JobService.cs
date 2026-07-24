using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using RecruitmentPlatform.Application.Common.Exceptions;
using RecruitmentPlatform.Application.Common.Models;
using RecruitmentPlatform.Application.DTOs.Jobs;
using RecruitmentPlatform.Application.Interfaces.Repositories;
using RecruitmentPlatform.Application.Interfaces.Services;
using RecruitmentPlatform.Domain.Entities;
using RecruitmentPlatform.Domain.Enums;

namespace RecruitmentPlatform.Application.Services;

/// <summary>
/// Read-only access to the public job board with search, filtering, sorting and pagination.
/// Only jobs in the <see cref="JobStatus.Open"/> state are exposed.
/// </summary>
public class JobService : IJobService
{
    private readonly IJobRepository _jobs;
    private readonly IMapper _mapper;

    public JobService(IJobRepository jobs, IMapper mapper)
    {
        _jobs = jobs;
        _mapper = mapper;
    }

    public async Task<PagedResult<JobListItemDto>> SearchAsync(JobQuery query, CancellationToken cancellationToken = default)
    {
        var q = _jobs.Query().Where(j => j.Status == JobStatus.Open);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            q = q.Where(j => j.Title.ToLower().Contains(term)
                             || j.Description.ToLower().Contains(term)
                             || j.Organization.Name.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(query.Location))
        {
            var loc = query.Location.Trim().ToLower();
            q = q.Where(j => j.Location != null && j.Location.ToLower().Contains(loc));
        }

        if (query.IsRemote.HasValue) q = q.Where(j => j.IsRemote == query.IsRemote.Value);
        if (query.EmploymentType.HasValue) q = q.Where(j => j.EmploymentType == query.EmploymentType.Value);
        if (query.ExperienceLevel.HasValue) q = q.Where(j => j.ExperienceLevel == query.ExperienceLevel.Value);
        if (query.MinSalary.HasValue) q = q.Where(j => j.SalaryMax >= query.MinSalary.Value);
        if (query.OrganizationId.HasValue) q = q.Where(j => j.OrganizationId == query.OrganizationId.Value);

        q = ApplySort(q, query.SortBy, query.SortDescending);

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ProjectTo<JobListItemDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return new PagedResult<JobListItemDto>(items, total, query.Page, query.PageSize);
    }

    public async Task<JobDetailDto> GetByIdAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var dto = await _jobs.Query()
            .Where(j => j.Id == jobId)
            .ProjectTo<JobDetailDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);

        return dto ?? throw new NotFoundException(nameof(Job), jobId);
    }

    private static IQueryable<Job> ApplySort(IQueryable<Job> q, string? sortBy, bool desc)
    {
        return (sortBy?.ToLowerInvariant()) switch
        {
            "title" => desc ? q.OrderByDescending(j => j.Title) : q.OrderBy(j => j.Title),
            "salary" => desc ? q.OrderByDescending(j => j.SalaryMax) : q.OrderBy(j => j.SalaryMax),
            _ => desc ? q.OrderBy(j => j.PostedAt) : q.OrderByDescending(j => j.PostedAt)
        };
    }
}
