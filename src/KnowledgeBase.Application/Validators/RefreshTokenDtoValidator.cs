using FluentValidation;
using KnowledgeBase.Application.DTOs;

namespace KnowledgeBase.Application.Validators;

public class RefreshTokenDtoValidator : AbstractValidator<RefreshTokenDto>
{
    public RefreshTokenDtoValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .MaximumLength(500);
    }
}
