using Microsoft.EntityFrameworkCore;
using RecruitmentPlatform.Application.Common.Exceptions;
using RecruitmentPlatform.Application.DTOs.Admin;
using RecruitmentPlatform.Application.Interfaces.Repositories;
using RecruitmentPlatform.Application.Interfaces.Services;
using RecruitmentPlatform.Domain.Entities;

namespace RecruitmentPlatform.Application.Services;

/// <summary>Administrator management of organizations and their departments.</summary>
public class AdminOrganizationService : IAdminOrganizationService
{
    private readonly IUnitOfWork _uow;

    public AdminOrganizationService(IUnitOfWork uow) => _uow = uow;

    public async Task<IReadOnlyList<AdminOrganizationDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var orgs = await _uow.Repository<Organization>().Query()
            .Include(o => o.Departments)
            .OrderBy(o => o.Name)
            .ToListAsync(cancellationToken);

        var jobsByOrg = (await _uow.Jobs.Query()
            .GroupBy(j => j.OrganizationId)
            .Select(g => new { OrgId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken))
            .ToDictionary(x => x.OrgId, x => x.Count);

        var jobsByDept = (await _uow.Jobs.Query()
            .Where(j => j.DepartmentId != null)
            .GroupBy(j => j.DepartmentId!.Value)
            .Select(g => new { DeptId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken))
            .ToDictionary(x => x.DeptId, x => x.Count);

        return orgs.Select(o => ToDto(o, jobsByOrg, jobsByDept)).ToList();
    }

    public async Task<AdminOrganizationDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var org = await _uow.Repository<Organization>().Query()
            .Include(o => o.Departments)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Organization), id);

        var jobsByOrg = new Dictionary<Guid, int> { [org.Id] = await _uow.Jobs.CountAsync(j => j.OrganizationId == id, cancellationToken) };
        var jobsByDept = (await _uow.Jobs.Query()
            .Where(j => j.OrganizationId == id && j.DepartmentId != null)
            .GroupBy(j => j.DepartmentId!.Value)
            .Select(g => new { DeptId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken))
            .ToDictionary(x => x.DeptId, x => x.Count);

        return ToDto(org, jobsByOrg, jobsByDept);
    }

    public async Task<AdminOrganizationDto> CreateAsync(SaveOrganizationRequest request, CancellationToken cancellationToken = default)
    {
        var org = new Organization();
        Apply(org, request);
        await _uow.Repository<Organization>().AddAsync(org, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return await GetAsync(org.Id, cancellationToken);
    }

    public async Task<AdminOrganizationDto> UpdateAsync(Guid id, SaveOrganizationRequest request, CancellationToken cancellationToken = default)
    {
        var org = await _uow.Repository<Organization>().Query().AsTracking()
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Organization), id);
        Apply(org, request);
        await _uow.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var org = await _uow.Repository<Organization>().GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Organization), id);

        if (await _uow.Jobs.AnyAsync(j => j.OrganizationId == id, cancellationToken))
        {
            throw new ConflictException("This organization has jobs and cannot be deleted.");
        }

        _uow.Repository<Organization>().Remove(org);
        await _uow.SaveChangesAsync(cancellationToken);
    }

    public async Task<AdminDepartmentDto> AddDepartmentAsync(Guid organizationId, SaveDepartmentRequest request, CancellationToken cancellationToken = default)
    {
        if (!await _uow.Repository<Organization>().AnyAsync(o => o.Id == organizationId, cancellationToken))
        {
            throw new NotFoundException(nameof(Organization), organizationId);
        }

        var department = new Department
        {
            OrganizationId = organizationId,
            Name = request.Name.Trim(),
            Description = request.Description,
        };
        await _uow.Repository<Department>().AddAsync(department, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return new AdminDepartmentDto(department.Id, department.Name, department.Description, 0);
    }

    public async Task RemoveDepartmentAsync(Guid organizationId, Guid departmentId, CancellationToken cancellationToken = default)
    {
        var department = await _uow.Repository<Department>()
            .FirstOrDefaultAsync(d => d.Id == departmentId && d.OrganizationId == organizationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Department), departmentId);

        _uow.Repository<Department>().Remove(department);
        await _uow.SaveChangesAsync(cancellationToken);
    }

    private static void Apply(Organization org, SaveOrganizationRequest request)
    {
        org.Name = request.Name.Trim();
        org.Description = request.Description;
        org.Industry = request.Industry;
        org.Website = request.Website;
        org.Location = request.Location;
        org.IsActive = request.IsActive;
    }

    private static AdminOrganizationDto ToDto(Organization o, IReadOnlyDictionary<Guid, int> jobsByOrg, IReadOnlyDictionary<Guid, int> jobsByDept)
        => new(
            o.Id, o.Name, o.Description, o.Industry, o.Website, o.Location, o.IsActive,
            o.Departments.Count,
            jobsByOrg.TryGetValue(o.Id, out var jc) ? jc : 0,
            o.Departments.OrderBy(d => d.Name)
                .Select(d => new AdminDepartmentDto(d.Id, d.Name, d.Description, jobsByDept.TryGetValue(d.Id, out var dc) ? dc : 0))
                .ToList());
}
