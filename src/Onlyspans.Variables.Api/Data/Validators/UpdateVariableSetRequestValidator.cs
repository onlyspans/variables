using FluentValidation;
using Onlyspans.Variables.Api.Data.Records;

namespace Onlyspans.Variables.Api.Data.Validators;

public sealed class UpdateVariableSetRequestValidator : AbstractValidator<UpdateVariableSetRequest>
{
    public UpdateVariableSetRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Variable set name cannot be empty if provided")
            .MaximumLength(256)
            .WithMessage("Variable set name must not exceed 256 characters")
            .When(x => x.Name != null);

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .WithMessage("Description must not exceed 2000 characters")
            .When(x => x.Description != null);

        // At least one field must be provided for an update
        RuleFor(x => x)
            .Must(x => x.Name != null || x.Description != null)
            .WithMessage("At least one field must be provided for update");
    }
}
