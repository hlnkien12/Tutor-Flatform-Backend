using FluentValidation;

namespace TutorPlatform.Application.Features.Profiles.Commands.UpdateTutorSubjects
{
    public class UpdateTutorSubjectsCommandValidator : AbstractValidator<UpdateTutorSubjectsCommand>
    {
        public UpdateTutorSubjectsCommandValidator()
        {
            RuleFor(x => x.UserId).NotEmpty().WithMessage("UserId is required.");
            RuleForEach(x => x.Subjects).ChildRules(subject => {
                subject.RuleFor(s => s.SubjectId).NotEmpty().WithMessage("SubjectId is required.");
                subject.RuleFor(s => s.ProficiencyLevel)
                    .Must(val => System.Enum.IsDefined(typeof(TutorPlatform.Domain.Enums.ProficiencyLevel), val))
                    .WithMessage("ProficiencyLevel must be 2 (Advanced), 3 (Expert), or 4 (Premium).");
                subject.RuleFor(s => s.HourlyCredits)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("HourlyCredits must be zero or positive.");
            });
        }
    }
}
