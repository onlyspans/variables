using FluentValidation;
using Onlyspans.Variables.Api.Data.Records;

namespace Onlyspans.Variables.Api.Data.Validators;

public sealed class CreateVariableSetRequestValidator : AbstractValidator<CreateVariableSetRequest>
{
    public CreateVariableSetRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Variable set name is required")
            .MaximumLength(256)
            .WithMessage("Variable set name must not exceed 256 characters");

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .WithMessage("Description must not exceed 2000 characters")
            .When(x => x.Description != null);
    }
}
