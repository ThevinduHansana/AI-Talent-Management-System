using System.Reflection;
using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using RecruitmentPlatform.Application.Interfaces.Infrastructure;
using RecruitmentPlatform.Application.Interfaces.Repositories;
using RecruitmentPlatform.Application.Interfaces.Services;
using RecruitmentPlatform.Application.Services;

namespace RecruitmentPlatform.Application;

/// <summary>
/// Registers application-layer services: AutoMapper profiles, FluentValidation validators and
/// the business services that orchestrate the domain.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddAutoMapper(assembly);
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<ICandidateService, CandidateService>();
        services.AddScoped<IJobService, JobService>();
        services.AddScoped<IApplicationService, ApplicationService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IMessageService, MessageService>();

        // Calendar integration. Both are pure functions over their input (no I/O, no state), so a
        // single instance is safely shared.
        services.AddSingleton<ICalendarLinkService, CalendarLinkService>();
        services.AddSingleton<IIcsGeneratorService, IcsGeneratorService>();
        // Shared by every scheduling path so the invitation is identical regardless of endpoint.
        services.AddScoped<IInterviewInvitationService, InterviewInvitationService>();

        // Recruiter module
        services.AddScoped<IRecruiterJobService, RecruiterJobService>();
        services.AddScoped<IRecruiterApplicationService, RecruiterApplicationService>();
        services.AddScoped<IInterviewService, InterviewService>();
        services.AddScoped<IInterviewSchedulingService, InterviewSchedulingService>();

        // Hiring-manager module
        services.AddScoped<IHiringManagerService, HiringManagerService>();

        // Administrator module
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<IAdminUserService, AdminUserService>();
        services.AddScoped<IAdminOrganizationService, AdminOrganizationService>();
        services.AddScoped<IAdminRoleService, AdminRoleService>();
        services.AddScoped<IAuditLogService, AuditLogService>();

        // AI services. The Claude-backed implementations are registered against the interfaces and
        // each takes its deterministic counterpart as a fallback, so the platform behaves the same
        // (with lower-quality output) when no Anthropic API key is configured.
        services.AddScoped<HeuristicMatchingService>();
        services.AddScoped(sp => new AiResumeService(
            sp.GetRequiredService<IUnitOfWork>(),
            sp.GetRequiredService<IFileStorageService>(),
            sp.GetRequiredService<ITextExtractor>(),
            // Deliberately the heuristic matcher: this instance is the offline fallback and must
            // not fan out into per-job model calls.
            sp.GetRequiredService<HeuristicMatchingService>(),
            sp.GetRequiredService<IMapper>()));

        services.AddScoped<IMatchingService, ClaudeMatchingService>();
        services.AddScoped<IAiService, ClaudeAiService>();

        return services;
    }
}
