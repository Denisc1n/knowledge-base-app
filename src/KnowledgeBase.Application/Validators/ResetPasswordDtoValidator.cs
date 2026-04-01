using FluentValidation;
using KnowledgeBase.Application.DTOs;

namespace KnowledgeBase.Application.Validators;

public class ResetPasswordDtoValidator : AbstractValidator<ResetPasswordDto>
{
    public ResetPasswordDtoValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(200);

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(200)
            .NotEqual(x => x.CurrentPassword)
            .WithMessage("The new password must be different from the current password.");
    }
}
