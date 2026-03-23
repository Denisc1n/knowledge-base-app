using FluentValidation;
using KnowledgeBase.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace KnowledgeBase.Application.Validators
{
    public class PatchNoteDtoValidator : AbstractValidator<PatchNoteDto>
    {
        public PatchNoteDtoValidator()
        {
            // Title
            RuleFor(x => x.Title)
                .NotEmpty()
                .MaximumLength(150)
                .When(x => x.Title is not null);

            // Content
            RuleFor(x => x.Content)
                .NotEmpty()
                .MaximumLength(10000)
                .When(x => x.Content is not null);

            // Category
            RuleFor(x => x.Category)
                .NotEmpty()
                .MaximumLength(80)
                .When(x => x.Category is not null);

            // Tags
            RuleFor(x => x.Tags)
                .Must(tags => tags!.All(tag => !string.IsNullOrWhiteSpace(tag)))
                .WithMessage("Tags cannot contain empty values.")
                .When(x => x.Tags is not null);

            RuleForEach(x => x.Tags!)
                .MaximumLength(40)
                .When(x => x.Tags is not null);

            // At least one field must be present
            RuleFor(x => x)
                .Must(HaveAtLeastOneField)
                .WithMessage("At least one field must be provided.");
        }

        private static bool HaveAtLeastOneField(PatchNoteDto dto)
        {
            return dto.Title is not null ||
                   dto.Content is not null ||
                   dto.Tags is not null ||
                   dto.Category is not null ||
                   dto.Status is not null;
        }
    }
}
