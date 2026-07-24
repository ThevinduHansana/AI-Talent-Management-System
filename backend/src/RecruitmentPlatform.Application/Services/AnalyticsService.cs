using Microsoft.EntityFrameworkCore;
using RecruitmentPlatform.Application.DTOs.Admin;
using RecruitmentPlatform.Application.Interfaces.Repositories;
using RecruitmentPlatform.Application.Interfaces.Services;
using RecruitmentPlatform.Domain.Entities;
using RecruitmentPlatform.Domain.Enums;

namespace RecruitmentPlatform.Application.Services;

/// <summary>
/// Aggregates platform-wide recruitment metrics for the administrator analytics dashboard.
/// </summary>
public class AnalyticsService : IAnalyticsService
{
    private const int MonthsWindow = 6;

    private readonly IUnitOfWork _uow;

    public AnalyticsService(IUnitOfWork uow) => _uow = uow;

    public async Task<AnalyticsOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        var totalUsers = await _uow.Users.CountAsync(cancellationToken: cancellationToken);
        var totalCandidates = await _uow.Repository<Candidate>().CountAsync(cancellationToken: cancellationToken);
        var totalRecruiters = await _uow.Repository<Recruiter>().CountAsync(cancellationToken: cancellationToken);
        var totalHiringManagers = await _uow.Repository<HiringManager>().CountAsync(cancellationToken: cancellationToken);

        var totalJobs = await _uow.Jobs.CountAsync(cancellationToken: cancellationToken);
        var activeJobs = await _uow.Jobs.CountAsync(j => j.Status == JobStatus.Open, cancellationToken);

        var totalApplications = await _uow.Applications.CountAsync(cancellationToken: cancellationToken);
        var hires = await _uow.Applications.CountAsync(a => a.Status == ApplicationStatus.Hired, cancellationToken);
        var totalInterviews = await _uow.Repository<InterviewSchedule>().CountAsync(cancellationToken: cancellationToken);

        var hiringRate = totalApplications == 0 ? 0 : Math.Round(hires * 100.0 / totalApplications, 1);

        var applicationsByStatus = await _uow.Applications.Query()
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        // Group by the direct FK column (translatable), then resolve skill names.
        var topSkillsRaw = await _uow.Repository<JobSkill>().Query()
            .GroupBy(js => js.SkillId)
            .Select(g => new { SkillId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(8)
            .ToListAsync(cancellationToken);
        var topSkillIds = topSkillsRaw.Select(x => x.SkillId).ToList();
        var skillNames = (await _uow.Skills.Query()
                .Where(s => topSkillIds.Contains(s.Id))
                .Select(s => new { s.Id, s.Name })
                .ToListAsync(cancellationToken))
            .ToDictionary(x => x.Id, x => x.Name);
        var topSkills = topSkillsRaw
            .Select(x => new LabelValueDto(skillNames.GetValueOrDefault(x.SkillId, "Unknown"), x.Count))
            .ToList();

        // Hired-per-department: project the department id (join in projection), group in memory.
        var hiredDeptIds = await _uow.Applications.Query()
            .Where(a => a.Status == ApplicationStatus.Hired && a.Job.DepartmentId != null)
            .Select(a => a.Job.DepartmentId!.Value)
            .ToListAsync(cancellationToken);
        var deptNames = (await _uow.Repository<Department>().Query()
                .Select(d => new { d.Id, d.Name })
                .ToListAsync(cancellationToken))
            .ToDictionary(x => x.Id, x => x.Name);
        var departmentHiring = hiredDeptIds
            .GroupBy(id => id)
            .Select(g => new LabelValueDto(deptNames.GetValueOrDefault(g.Key, "Unknown"), g.Count()))
            .OrderByDescending(x => x.Value)
            .ToList();

        var since = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-(MonthsWindow - 1));

        var monthlyApplicationsRaw = await _uow.Applications.Query()
            .Where(a => a.AppliedAt >= since)
            .GroupBy(a => new { a.AppliedAt.Year, a.AppliedAt.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .ToListAsync(cancellationToken);

        // Hire dates use a coalesce that does not translate to SQL GROUP BY; group in memory.
        var hireDates = await _uow.Applications.Query()
            .Where(a => a.Status == ApplicationStatus.Hired)
            .Select(a => a.StatusChangedAt ?? a.AppliedAt)
            .ToListAsync(cancellationToken);
        var monthlyHiresRaw = hireDates
            .Where(d => d >= since)
            .GroupBy(d => new { d.Year, d.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() });

        var monthlyApplications = BuildSeries(since, monthlyApplicationsRaw.Select(x => (x.Year, x.Month, x.Count)));
        var monthlyHires = BuildSeries(since, monthlyHiresRaw.Select(x => (x.Year, x.Month, x.Count)));

        var recruiterPerformance = await BuildRecruiterPerformanceAsync(cancellationToken);

        return new AnalyticsOverviewDto(
            totalUsers, totalCandidates, totalRecruiters, totalHiringManagers,
            activeJobs, totalJobs, totalApplications, totalInterviews, hires, hiringRate,
            applicationsByStatus.Select(s => new StatusCountDto(s.Status.ToString(), s.Count)).ToList(),
            topSkills, departmentHiring, monthlyApplications, monthlyHires, recruiterPerformance);
    }

    private async Task<IReadOnlyList<RecruiterPerformanceDto>> BuildRecruiterPerformanceAsync(CancellationToken cancellationToken)
    {
        var recruiters = await _uow.Repository<Recruiter>().Query()
            .Select(r => new { r.Id, Name = r.User.FirstName + " " + r.User.LastName })
            .ToListAsync(cancellationToken);

        var jobsPerRecruiter = (await _uow.Jobs.Query()
            .GroupBy(j => j.RecruiterId)
            .Select(g => new { RecruiterId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken))
            .ToDictionary(x => x.RecruiterId, x => x.Count);

        // Project recruiter id + hired flag (join in projection), aggregate in memory.
        var appRows = await _uow.Applications.Query()
            .Select(a => new { a.Job.RecruiterId, IsHired = a.Status == ApplicationStatus.Hired })
            .ToListAsync(cancellationToken);
        var appsPerRecruiter = appRows
            .GroupBy(x => x.RecruiterId)
            .ToDictionary(g => g.Key, g => (Apps: g.Count(), Hires: g.Count(x => x.IsHired)));

        return recruiters
            .Select(r =>
            {
                jobsPerRecruiter.TryGetValue(r.Id, out var jobs);
                appsPerRecruiter.TryGetValue(r.Id, out var stats);
                return new RecruiterPerformanceDto(r.Name, jobs, stats.Apps, stats.Hires);
            })
            .OrderByDescending(r => r.Applications)
            .Take(6)
            .ToList();
    }

    /// <summary>Builds a continuous month-by-month series (zero-filled) from grouped counts.</summary>
    private static IReadOnlyList<TimeSeriesPointDto> BuildSeries(DateTime since, IEnumerable<(int Year, int Month, int Count)> raw)
    {
        var lookup = raw.ToDictionary(x => (x.Year, x.Month), x => x.Count);
        var series = new List<TimeSeriesPointDto>();
        for (var i = 0; i < MonthsWindow; i++)
        {
            var month = since.AddMonths(i);
            lookup.TryGetValue((month.Year, month.Month), out var count);
            series.Add(new TimeSeriesPointDto(month.ToString("yyyy-MM"), count));
        }
        return series;
    }
}
