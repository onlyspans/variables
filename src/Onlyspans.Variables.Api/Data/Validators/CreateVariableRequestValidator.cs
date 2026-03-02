using FluentValidation;
using Onlyspans.Variables.Api.Abstractions.Services;
using Onlyspans.Variables.Api.Data.Records;

namespace Onlyspans.Variables.Api.Data.Validators;

public sealed class CreateVariableRequestValidator : AbstractValidator<CreateVariableRequest>
{
    public CreateVariableRequestValidator(IProjectsClient projectsClient)
    {
        RuleFor(x => x.Key)
            .NotEmpty()
            .WithMessage("Variable key is required")
            .MaximumLength(256)
            .WithMessage("Variable key must not exceed 256 characters")
            .Matches("^[a-zA-Z0-9_.-]+$")
            .WithMessage("Variable key must contain only alphanumeric characters, underscores, dots, and hyphens");

        RuleFor(x => x.Value)
            .NotNull()
            .WithMessage("Variable value is required")
            .NotEmpty()
            .WithMessage("Variable value cannot be empty");

        // Note: ProjectId validation is done at the endpoint level since it comes from the route
        // VariableSetId validation would require checking against the database
    }
}
