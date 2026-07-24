using Microsoft.EntityFrameworkCore;
using RecruitmentPlatform.Application.Interfaces.Infrastructure;
using RecruitmentPlatform.Domain.Common;
using RecruitmentPlatform.Domain.Entities;
using RecruitmentPlatform.Domain.Enums;

namespace RecruitmentPlatform.Infrastructure.Data.Seed;

/// <summary>
/// Seeds baseline reference data (roles, permissions, an administrator) and a small amount of
/// demonstration data (organization, recruiter, skills, open jobs) so the platform is usable
/// immediately after a fresh migration. All operations are idempotent.
/// </summary>
public static class DatabaseSeeder
{
    public const string AdminEmail = "admin@recruitment.local";
    public const string AdminPassword = "Admin@12345";
    public const string RecruiterEmail = "recruiter@recruitment.local";
    public const string RecruiterPassword = "Recruiter@12345";
    public const string HiringManagerEmail = "manager@recruitment.local";
    public const string HiringManagerPassword = "Manager@12345";

    public static async Task SeedAsync(ApplicationDbContext context, IPasswordHasher passwordHasher, CancellationToken cancellationToken = default)
    {
        var roles = await SeedRolesAsync(context, cancellationToken);
        await SeedPermissionsAsync(context, roles[RoleNames.Administrator], cancellationToken);
        await SeedAdminAsync(context, passwordHasher, roles[RoleNames.Administrator], cancellationToken);
        var skills = await SeedSkillsAsync(context, cancellationToken);
        var organization = await SeedOrganizationAsync(context, cancellationToken);
        var recruiter = await SeedRecruiterAsync(context, passwordHasher, roles[RoleNames.Recruiter], organization, cancellationToken);
        await SeedJobsAsync(context, organization, recruiter, skills, cancellationToken);
        await SeedHiringManagerAsync(context, passwordHasher, roles[RoleNames.HiringManager], organization, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task<Dictionary<string, Role>> SeedRolesAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        var existing = await context.Roles.ToListAsync(cancellationToken);
        var map = existing.ToDictionary(r => r.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var name in RoleNames.All)
        {
            if (!map.ContainsKey(name))
            {
                var role = new Role { Name = name, NormalizedName = name.ToUpperInvariant(), Description = $"{name} role" };
                await context.Roles.AddAsync(role, cancellationToken);
                map[name] = role;
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        return map;
    }

    private static async Task SeedPermissionsAsync(ApplicationDbContext context, Role administrator, CancellationToken cancellationToken)
    {
        var definitions = new (string Name, string Category)[]
        {
            ("users.manage", "Administration"),
            ("roles.manage", "Administration"),
            ("organizations.manage", "Administration"),
            ("departments.manage", "Administration"),
            ("audit.view", "Administration"),
            ("analytics.view", "Analytics"),
            ("jobs.create", "Recruitment"),
            ("jobs.edit", "Recruitment"),
            ("jobs.delete", "Recruitment"),
            ("candidates.search", "Recruitment"),
            ("applications.review", "Recruitment"),
            ("interviews.schedule", "Recruitment"),
            ("evaluations.submit", "Hiring"),
            ("hiring.decide", "Hiring")
        };

        var existingNames = await context.Permissions.Select(p => p.Name).ToListAsync(cancellationToken);
        var toAdd = definitions.Where(d => !existingNames.Contains(d.Name))
            .Select(d => new Permission { Name = d.Name, Category = d.Category, Description = d.Name })
            .ToList();

        if (toAdd.Count > 0)
        {
            await context.Permissions.AddRangeAsync(toAdd, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        // Grant every permission to the Administrator role.
        var allPermissions = await context.Permissions.ToListAsync(cancellationToken);
        var grantedIds = await context.RolePermissions
            .Where(rp => rp.RoleId == administrator.Id)
            .Select(rp => rp.PermissionId)
            .ToListAsync(cancellationToken);

        var newGrants = allPermissions
            .Where(p => !grantedIds.Contains(p.Id))
            .Select(p => new RolePermission { RoleId = administrator.Id, PermissionId = p.Id })
            .ToList();

        if (newGrants.Count > 0)
        {
            await context.RolePermissions.AddRangeAsync(newGrants, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task SeedAdminAsync(ApplicationDbContext context, IPasswordHasher passwordHasher, Role administrator, CancellationToken cancellationToken)
    {
        var normalized = AdminEmail.ToUpperInvariant();
        if (await context.Users.AnyAsync(u => u.NormalizedEmail == normalized, cancellationToken))
        {
            return;
        }

        var admin = new User
        {
            FirstName = "System",
            LastName = "Administrator",
            Email = AdminEmail,
            NormalizedEmail = normalized,
            PasswordHash = passwordHasher.Hash(AdminPassword),
            IsActive = true,
            IsEmailConfirmed = true
        };
        admin.UserRoles.Add(new UserRole { Role = administrator });
        await context.Users.AddAsync(admin, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task<Dictionary<string, Skill>> SeedSkillsAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        var catalog = new (string Name, string Category)[]
        {
            ("C#", "Programming"), (".NET", "Framework"), ("ASP.NET Core", "Framework"),
            ("React", "Frontend"), ("JavaScript", "Programming"), ("TypeScript", "Programming"),
            ("SQL", "Database"), ("PostgreSQL", "Database"), ("Docker", "DevOps"),
            ("Kubernetes", "DevOps"), ("AWS", "Cloud"), ("Communication", "Soft Skills"),
            ("Leadership", "Soft Skills"), ("Python", "Programming")
        };

        var existing = await context.Skills.ToListAsync(cancellationToken);
        var map = existing.ToDictionary(s => s.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var (name, category) in catalog)
        {
            if (!map.ContainsKey(name))
            {
                var skill = new Skill { Name = name, NormalizedName = name.ToUpperInvariant(), Category = category };
                await context.Skills.AddAsync(skill, cancellationToken);
                map[name] = skill;
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        return map;
    }

    private static async Task<Organization> SeedOrganizationAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        var organization = await context.Organizations
            .Include(o => o.Departments)
            .FirstOrDefaultAsync(o => o.Name == "Acme Global", cancellationToken);

        if (organization is not null)
        {
            return organization;
        }

        organization = new Organization
        {
            Name = "Acme Global",
            Description = "A multinational technology and consulting company.",
            Industry = "Technology",
            Website = "https://acme.example",
            Location = "New York, USA",
            IsActive = true,
            Departments = new List<Department>
            {
                new() { Name = "Engineering", Description = "Product engineering" },
                new() { Name = "Sales", Description = "Global sales" }
            }
        };
        await context.Organizations.AddAsync(organization, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return organization;
    }

    private static async Task<Recruiter> SeedRecruiterAsync(ApplicationDbContext context, IPasswordHasher passwordHasher, Role recruiterRole, Organization organization, CancellationToken cancellationToken)
    {
        var normalized = RecruiterEmail.ToUpperInvariant();
        var user = await context.Users
            .Include(u => u.Recruiter)
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalized, cancellationToken);

        if (user?.Recruiter is not null)
        {
            return user.Recruiter;
        }

        user = new User
        {
            FirstName = "Rita",
            LastName = "Recruiter",
            Email = RecruiterEmail,
            NormalizedEmail = normalized,
            PasswordHash = passwordHasher.Hash(RecruiterPassword),
            IsActive = true,
            IsEmailConfirmed = true,
            OrganizationId = organization.Id
        };
        user.UserRoles.Add(new UserRole { Role = recruiterRole });
        user.Recruiter = new Recruiter
        {
            OrganizationId = organization.Id,
            JobTitle = "Senior Technical Recruiter"
        };
        await context.Users.AddAsync(user, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return user.Recruiter;
    }

    private static async Task SeedHiringManagerAsync(ApplicationDbContext context, IPasswordHasher passwordHasher, Role hiringManagerRole, Organization organization, CancellationToken cancellationToken)
    {
        var normalized = HiringManagerEmail.ToUpperInvariant();
        var user = await context.Users
            .Include(u => u.HiringManager)
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalized, cancellationToken);

        if (user?.HiringManager is not null)
        {
            return;
        }

        var engineering = organization.Departments.FirstOrDefault(d => d.Name == "Engineering");

        user = new User
        {
            FirstName = "Henry",
            LastName = "Manager",
            Email = HiringManagerEmail,
            NormalizedEmail = normalized,
            PasswordHash = passwordHasher.Hash(HiringManagerPassword),
            IsActive = true,
            IsEmailConfirmed = true,
            OrganizationId = organization.Id,
            DepartmentId = engineering?.Id,
        };
        user.UserRoles.Add(new UserRole { Role = hiringManagerRole });
        user.HiringManager = new HiringManager
        {
            OrganizationId = organization.Id,
            DepartmentId = engineering?.Id,
            JobTitle = "Engineering Hiring Manager",
        };
        await context.Users.AddAsync(user, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedJobsAsync(ApplicationDbContext context, Organization organization, Recruiter recruiter, Dictionary<string, Skill> skills, CancellationToken cancellationToken)
    {
        if (await context.Jobs.AnyAsync(cancellationToken))
        {
            return;
        }

        var engineering = organization.Departments.FirstOrDefault(d => d.Name == "Engineering");

        var jobs = new List<Job>
        {
            new()
            {
                Title = "Senior Full Stack Engineer",
                Description = "Build and scale our recruitment platform across the stack using .NET and React.",
                Responsibilities = "Design APIs, build UI, mentor engineers.",
                Requirements = "5+ years experience with C# and React.",
                EmploymentType = EmploymentType.FullTime,
                ExperienceLevel = ExperienceLevel.Senior,
                Location = "New York, USA",
                IsRemote = true,
                SalaryMin = 120000, SalaryMax = 160000, Currency = "USD",
                Vacancies = 2,
                Status = JobStatus.Open,
                PostedAt = DateTime.UtcNow.AddDays(-3),
                ClosingDate = DateTime.UtcNow.AddDays(27),
                OrganizationId = organization.Id,
                DepartmentId = engineering?.Id,
                RecruiterId = recruiter.Id,
                JobSkills = new List<JobSkill>
                {
                    new() { SkillId = skills["C#"].Id, IsRequired = true, MinimumProficiency = ProficiencyLevel.Advanced, Weight = 9 },
                    new() { SkillId = skills["React"].Id, IsRequired = true, MinimumProficiency = ProficiencyLevel.Advanced, Weight = 8 },
                    new() { SkillId = skills["PostgreSQL"].Id, IsRequired = false, MinimumProficiency = ProficiencyLevel.Intermediate, Weight = 6 }
                }
            },
            new()
            {
                Title = "DevOps Engineer",
                Description = "Own our CI/CD pipelines and cloud infrastructure on AWS with Kubernetes.",
                Responsibilities = "Automate deployments, manage clusters, improve reliability.",
                Requirements = "Strong Docker/Kubernetes and AWS experience.",
                EmploymentType = EmploymentType.FullTime,
                ExperienceLevel = ExperienceLevel.Mid,
                Location = "Remote",
                IsRemote = true,
                SalaryMin = 100000, SalaryMax = 130000, Currency = "USD",
                Vacancies = 1,
                Status = JobStatus.Open,
                PostedAt = DateTime.UtcNow.AddDays(-1),
                ClosingDate = DateTime.UtcNow.AddDays(29),
                OrganizationId = organization.Id,
                DepartmentId = engineering?.Id,
                RecruiterId = recruiter.Id,
                JobSkills = new List<JobSkill>
                {
                    new() { SkillId = skills["Docker"].Id, IsRequired = true, MinimumProficiency = ProficiencyLevel.Advanced, Weight = 9 },
                    new() { SkillId = skills["Kubernetes"].Id, IsRequired = true, MinimumProficiency = ProficiencyLevel.Advanced, Weight = 9 },
                    new() { SkillId = skills["AWS"].Id, IsRequired = true, MinimumProficiency = ProficiencyLevel.Intermediate, Weight = 7 }
                }
            }
        };

        await context.Jobs.AddRangeAsync(jobs, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}
