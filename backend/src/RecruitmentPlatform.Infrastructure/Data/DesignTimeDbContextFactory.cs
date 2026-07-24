using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RecruitmentPlatform.Infrastructure.Data;

/// <summary>
/// Enables `dotnet ef` tooling to construct the context at design time without running the API.
/// Uses the DESIGN_TIME_CONNECTION environment variable when present, otherwise the local
/// development connection string.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("DESIGN_TIME_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=recruitment_platform;Username=recruit_app;Password=Recruit@2026!";

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new ApplicationDbContext(options);
    }
}
