using FluentValidation;
using RecruitmentPlatform.Application.DTOs.Recruiter;

namespace RecruitmentPlatform.Application.Validators.Jobs;

public class SaveJobRequestValidator : AbstractValidator<SaveJobRequest>
{
    public SaveJobRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(8000);
        RuleFor(x => x.Currency).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Vacancies).InclusiveBetween(1, 999);
        RuleFor(x => x.SalaryMin).GreaterThanOrEqualTo(0).When(x => x.SalaryMin.HasValue);
        RuleFor(x => x.SalaryMax).GreaterThanOrEqualTo(0).When(x => x.SalaryMax.HasValue);
        RuleFor(x => x)
            .Must(x => !(x.SalaryMin.HasValue && x.SalaryMax.HasValue) || x.SalaryMax >= x.SalaryMin)
            .WithMessage("Maximum salary must be greater than or equal to minimum salary.");
        RuleForEach(x => x.Skills).ChildRules(s =>
        {
            s.RuleFor(i => i.SkillName).NotEmpty().MaximumLength(100);
            s.RuleFor(i => i.Weight).InclusiveBetween(1, 10);
        });
    }
}

public class ScheduleInterviewRequestValidator : AbstractValidator<ScheduleInterviewRequest>
{
    public ScheduleInterviewRequestValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ScheduledAt).GreaterThan(DateTime.UtcNow).WithMessage("Interview time must be in the future.");
        RuleFor(x => x.DurationMinutes).InclusiveBetween(15, 480);
    }
}
