using FluentValidation;
using KnowledgeBase.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace KnowledgeBase.Application.Validators
{
    public class CreateNoteDtoValidator : AbstractValidator<CreateNoteDto>
    {
        public CreateNoteDtoValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty()
                .MaximumLength(150);

            RuleFor(x => x.Content)
                .NotEmpty()
                .MaximumLength(10000);

            RuleFor(x => x.Category)
                .NotEmpty()
                .MaximumLength(80);

            RuleForEach(x => x.Tags)
                .MaximumLength(40);
        }
    }
}
