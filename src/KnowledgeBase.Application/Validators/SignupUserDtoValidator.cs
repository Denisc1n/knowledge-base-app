using FluentValidation;
using KnowledgeBase.Application.DTOs;

namespace KnowledgeBase.Application.Validators;

public class SignupUserDtoValidator : AbstractValidator<SignupUserDto>
{
    public SignupUserDtoValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(80);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MaximumLength(80);

        RuleFor(x => x.Username)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(50)
            .Matches("^[a-zA-Z0-9._-]+$")
            .WithMessage("Username can contain letters, numbers, dots, underscores, and hyphens.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .MaximumLength(320)
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(200);
    }
}
