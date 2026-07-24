using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RecruitmentPlatform.Application.Common.Exceptions;
using RecruitmentPlatform.Application.Common.Models;
using RecruitmentPlatform.Application.DTOs.Applications;
using RecruitmentPlatform.Application.Mappings;
using RecruitmentPlatform.Application.Services;
using RecruitmentPlatform.Domain.Entities;
using RecruitmentPlatform.Domain.Enums;
using RecruitmentPlatform.Infrastructure.Data;
using RecruitmentPlatform.Infrastructure.Repositories;
using RecruitmentPlatform.Infrastructure.Services;
using Xunit;

namespace RecruitmentPlatform.Tests.Integration;

/// <summary>
/// Exercises the candidate application workflow against an in-memory database using the real
/// repositories, unit of work and AutoMapper configuration.
/// </summary>
public class ApplicationServiceTests
{
    private static IMapper CreateMapper()
    {
        // Build the mapper through the same DI path used in production for API parity.
        var provider = new ServiceCollection()
            .AddAutoMapper(typeof(MappingProfile).Assembly)
            .BuildServiceProvider();
        return provider.GetRequiredService<IMapper>();
    }

    private static ApplicationDbContext CreateContext()
        => new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"apptests-{Guid.NewGuid()}")
            .Options);

    /// <summary>
    /// Builds the notification service with the logging (no-op) email and SMS channels, so the
    /// fan-out path is exercised without any network I/O.
    /// </summary>
    private static NotificationService CreateNotifications(ApplicationDbContext ctx)
        => new(
            new UnitOfWork(ctx),
            new LoggingEmailService(NullLogger<LoggingEmailService>.Instance),
            new LoggingSmsService(NullLogger<LoggingSmsService>.Instance),
            Options.Create(new NotificationSettings()),
            Options.Create(new EmailSettings()),
            NullLogger<NotificationService>.Instance);

    private static async Task<(Guid userId, Guid jobId)> SeedAsync(ApplicationDbContext ctx)
    {
        var org = new Organization { Name = "Acme" };
        var recruiterUser = new User { FirstName = "Ruth", LastName = "Recruiter", Email = "r@acme.com", NormalizedEmail = "R@ACME.COM", PasswordHash = "x" };
        var recruiter = new Recruiter { User = recruiterUser, Organization = org };
        var candidateUser = new User { FirstName = "Cara", LastName = "Candidate", Email = "c@x.com", NormalizedEmail = "C@X.COM", PasswordHash = "x" };
        var candidate = new Candidate { User = candidateUser };
        var job = new Job
        {
            Title = "Engineer", Description = "Build things", Status = JobStatus.Open,
            Organization = org, Recruiter = recruiter, Currency = "USD",
        };

        ctx.AddRange(org, recruiter, candidate, job);
        await ctx.SaveChangesAsync();
        return (candidateUser.Id, job.Id);
    }

    [Fact]
    public async Task ApplyAsync_CreatesApplication_WithAppliedStatus()
    {
        await using var ctx = CreateContext();
        var (userId, jobId) = await SeedAsync(ctx);
        var service = new ApplicationService(new UnitOfWork(ctx), CreateMapper(), CreateNotifications(ctx), new AuditService(new UnitOfWork(ctx)));

        var result = await service.ApplyAsync(userId, new ApplyToJobRequest(jobId, null, "Hire me"));

        result.Status.Should().Be(ApplicationStatus.Applied);
        result.JobTitle.Should().Be("Engineer");
        result.OrganizationName.Should().Be("Acme");
        (await ctx.Applications.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task ApplyAsync_Throws_OnDuplicateApplication()
    {
        await using var ctx = CreateContext();
        var (userId, jobId) = await SeedAsync(ctx);
        var service = new ApplicationService(new UnitOfWork(ctx), CreateMapper(), CreateNotifications(ctx), new AuditService(new UnitOfWork(ctx)));

        await service.ApplyAsync(userId, new ApplyToJobRequest(jobId, null, null));

        var act = () => service.ApplyAsync(userId, new ApplyToJobRequest(jobId, null, null));
        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task WithdrawAsync_SetsStatusToWithdrawn()
    {
        await using var ctx = CreateContext();
        var (userId, jobId) = await SeedAsync(ctx);
        var service = new ApplicationService(new UnitOfWork(ctx), CreateMapper(), CreateNotifications(ctx), new AuditService(new UnitOfWork(ctx)));
        var application = await service.ApplyAsync(userId, new ApplyToJobRequest(jobId, null, null));

        await service.WithdrawAsync(userId, application.Id);

        var reloaded = await service.GetApplicationAsync(userId, application.Id);
        reloaded.Status.Should().Be(ApplicationStatus.Withdrawn);
    }

    [Fact]
    public async Task SaveJob_IsIdempotent()
    {
        await using var ctx = CreateContext();
        var (userId, jobId) = await SeedAsync(ctx);
        var service = new ApplicationService(new UnitOfWork(ctx), CreateMapper(), CreateNotifications(ctx), new AuditService(new UnitOfWork(ctx)));

        await service.SaveJobAsync(userId, jobId);
        await service.SaveJobAsync(userId, jobId);

        var saved = await service.GetSavedJobsAsync(userId);
        saved.Should().HaveCount(1);
    }
}
