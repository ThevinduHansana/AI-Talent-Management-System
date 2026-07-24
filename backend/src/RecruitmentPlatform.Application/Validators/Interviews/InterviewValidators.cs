using FluentValidation;
using RecruitmentPlatform.Application.DTOs.Interviews;

namespace RecruitmentPlatform.Application.Validators.Interviews;

/// <summary>Shared rules for scheduling and rescheduling an interview.</summary>
public static class InterviewRules
{
    public const int MinDurationMinutes = 15;
    public const int MaxDurationMinutes = 240;

    /// <summary>
    /// Small tolerance on "must be in the future" so a request that spent a few seconds in flight,
    /// or a client whose clock is marginally behind, isn't rejected for a date the user typed as
    /// valid.
    /// </summary>
    private static readonly TimeSpan ClockSkew = TimeSpan.FromMinutes(1);

    public static IRuleBuilderOptions<T, DateTime> MustBeInTheFuture<T>(this IRuleBuilder<T, DateTime> rule)
        => rule.Must(date => ToUtc(date) > DateTime.UtcNow.Subtract(ClockSkew))
               .WithMessage("The interview date must be in the future.");

    public static IRuleBuilderOptions<T, int> MustBeAValidDuration<T>(this IRuleBuilder<T, int> rule)
        => rule.InclusiveBetween(MinDurationMinutes, MaxDurationMinutes)
               .WithMessage($"Duration must be between {MinDurationMinutes} and {MaxDurationMinutes} minutes.");

    /// <summary>
    /// Treats an untagged DateTime as UTC. The API contract is UTC, and a client may serialise
    /// without a trailing 'Z'.
    /// </summary>
    public static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };
}

public class CreateInterviewDtoValidator : AbstractValidator<CreateInterviewDto>
{
    public CreateInterviewDtoValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.InterviewDate).MustBeInTheFuture();
        RuleFor(x => x.DurationMinutes).MustBeAValidDuration();
        RuleFor(x => x.Location).MaximumLength(256);
        RuleFor(x => x.MeetingLink).MaximumLength(512);
        RuleFor(x => x.Notes).MaximumLength(2000);

        RuleFor(x => x.MeetingLink)
            .Must(BeAnAbsoluteHttpUrl)
            .When(x => !string.IsNullOrWhiteSpace(x.MeetingLink))
            .WithMessage("The meeting link must be an absolute http(s) URL.");
    }

    /// <summary>
    /// A relative or malformed link would be embedded in the invitation email and the .ics URL
    /// property, where it silently fails for the candidate — so reject it at the edge.
    /// </summary>
    internal static bool BeAnAbsoluteHttpUrl(string? value)
        => Uri.TryCreate(value, UriKind.Absolute, out var uri)
           && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}

public class UpdateInterviewDtoValidator : AbstractValidator<UpdateInterviewDto>
{
    public UpdateInterviewDtoValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.InterviewDate).MustBeInTheFuture();
        RuleFor(x => x.DurationMinutes).MustBeAValidDuration();
        RuleFor(x => x.Location).MaximumLength(256);
        RuleFor(x => x.MeetingLink).MaximumLength(512);
        RuleFor(x => x.Notes).MaximumLength(2000);

        RuleFor(x => x.MeetingLink)
            .Must(CreateInterviewDtoValidator.BeAnAbsoluteHttpUrl)
            .When(x => !string.IsNullOrWhiteSpace(x.MeetingLink))
            .WithMessage("The meeting link must be an absolute http(s) URL.");
    }
}
