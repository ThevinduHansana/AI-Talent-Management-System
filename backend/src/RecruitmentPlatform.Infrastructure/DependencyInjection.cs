using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RecruitmentPlatform.Application.Common.Models;
using RecruitmentPlatform.Application.Interfaces.Infrastructure;
using RecruitmentPlatform.Application.Interfaces.Repositories;
using RecruitmentPlatform.Domain.Common;
using RecruitmentPlatform.Infrastructure.Authentication;
using RecruitmentPlatform.Infrastructure.Data;
using RecruitmentPlatform.Infrastructure.Repositories;
using RecruitmentPlatform.Infrastructure.Services;

namespace RecruitmentPlatform.Infrastructure;

/// <summary>
/// Registers infrastructure concerns: the EF Core context, repositories/unit of work,
/// authentication primitives and swappable external service abstractions.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not configured.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                // Retry transient failures — cloud/Neon cold-start drops AND intermittent DNS
                // resolution failures for the pooler host — instead of surfacing them as 500s.
                npgsql.ExecutionStrategy(dependencies =>
                    new ResilientNpgsqlExecutionStrategy(dependencies, maxRetryCount: 8, maxRetryDelay: System.TimeSpan.FromSeconds(8)));
            }));

        // Options
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<StorageSettings>(configuration.GetSection(StorageSettings.SectionName));
        services.Configure<AiSettings>(configuration.GetSection(AiSettings.SectionName));
        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
        services.Configure<SmsSettings>(configuration.GetSection(SmsSettings.SectionName));
        services.Configure<NotificationSettings>(configuration.GetSection(NotificationSettings.SectionName));

        // Persistence: unit of work plus directly-injectable repositories.
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICandidateRepository, CandidateRepository>();
        services.AddScoped<IJobRepository, JobRepository>();
        services.AddScoped<IApplicationRepository, ApplicationRepository>();
        services.AddScoped<ISkillRepository, SkillRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        // Authentication
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();

        // External service abstractions (swappable: local <-> cloud).
        services.AddSingleton<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<ICalendarService, LoggingCalendarService>();
        services.AddSingleton<ITextExtractor, FileTextExtractor>();

        // Communication channels. Both implementations degrade to a log line when their provider
        // is unconfigured, so the platform behaves identically with no SMTP host or Twilio account.
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddHttpClient(nameof(TwilioSmsService));
        services.AddScoped<ISmsService, TwilioSmsService>();

        // Automated interview reminders (24h / 1h before start, configurable).
        services.AddHostedService<InterviewReminderService>();

        // LLM provider (Claude). Stateless and thread-safe, so a single instance is reused.
        services.AddSingleton<IAiCompletionService, ClaudeCompletionService>();

        return services;
    }
}
