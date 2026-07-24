using FluentValidation;
using RecruitmentPlatform.Application.DTOs.HiringManager;

namespace RecruitmentPlatform.Application.Validators.HiringManager;

public class SubmitEvaluationRequestValidator : AbstractValidator<SubmitEvaluationRequest>
{
    public SubmitEvaluationRequestValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.TechnicalScore).InclusiveBetween(0, 100);
        RuleFor(x => x.CommunicationScore).InclusiveBetween(0, 100);
        RuleFor(x => x.CultureFitScore).InclusiveBetween(0, 100);
        RuleFor(x => x.Comments).MaximumLength(2000);
    }
}

public class SubmitInterviewFeedbackRequestValidator : AbstractValidator<SubmitInterviewFeedbackRequest>
{
    public SubmitInterviewFeedbackRequestValidator()
    {
        RuleFor(x => x.InterviewScheduleId).NotEmpty();
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Strengths).MaximumLength(2000);
        RuleFor(x => x.Weaknesses).MaximumLength(2000);
        RuleFor(x => x.Comments).MaximumLength(2000);
    }
}
