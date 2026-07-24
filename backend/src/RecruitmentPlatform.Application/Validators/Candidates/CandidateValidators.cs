using FluentValidation;
using RecruitmentPlatform.Application.DTOs.Candidates;

namespace RecruitmentPlatform.Application.Validators.Candidates;

public class UpdateCandidateProfileValidator : AbstractValidator<UpdateCandidateProfileRequest>
{
    public UpdateCandidateProfileValidator()
    {
        RuleFor(x => x.Headline).MaximumLength(200);
        RuleFor(x => x.Summary).MaximumLength(4000);
        RuleFor(x => x.YearsOfExperience).InclusiveBetween(0, 60);
        RuleFor(x => x.ExpectedSalary).GreaterThanOrEqualTo(0).When(x => x.ExpectedSalary.HasValue);
        RuleFor(x => x.DateOfBirth)
            .LessThan(DateTime.UtcNow.AddYears(-14))
            .When(x => x.DateOfBirth.HasValue)
            .WithMessage("Candidate must be at least 14 years old.");
    }
}

public class AddCandidateSkillValidator : AbstractValidator<AddCandidateSkillRequest>
{
    public AddCandidateSkillValidator()
    {
        RuleFor(x => x.SkillName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.YearsOfExperience).InclusiveBetween(0, 60);
    }
}

public class SaveEducationValidator : AbstractValidator<SaveEducationRequest>
{
    public SaveEducationValidator()
    {
        RuleFor(x => x.Institution).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Degree).NotEmpty().MaximumLength(200);
        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .When(x => x.EndDate.HasValue)
            .WithMessage("End date cannot be before start date.");
    }
}

public class SaveExperienceValidator : AbstractValidator<SaveExperienceRequest>
{
    public SaveExperienceValidator()
    {
        RuleFor(x => x.Company).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .When(x => x.EndDate.HasValue)
            .WithMessage("End date cannot be before start date.");
    }
}

public class SaveCertificateValidator : AbstractValidator<SaveCertificateRequest>
{
    public SaveCertificateValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.IssuingOrganization).MaximumLength(200);
    }
}
