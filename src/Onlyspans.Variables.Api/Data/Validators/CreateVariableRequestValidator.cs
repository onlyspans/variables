using FluentValidation;
using Onlyspans.Variables.Api.Abstractions.Services;
using Onlyspans.Variables.Api.Data.Records;

namespace Onlyspans.Variables.Api.Data.Validators;

public sealed class CreateVariableRequestValidator : AbstractValidator<CreateVariableRequest>
{
    public CreateVariableRequestValidator(
        ITargetsPlaneClient targetsPlaneClient,
        IProjectsClient projectsClient)
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

        RuleFor(x => x.EnvironmentId)
            .MustAsync(async (environmentId, ct) =>
            {
                if (!environmentId.HasValue)
                    return true; // Unscoped variables are allowed

                return await targetsPlaneClient.EnvironmentExistsAsync(environmentId.Value, ct);
            })
            .WithMessage("Environment does not exist")
            .When(x => x.EnvironmentId.HasValue);

        // Note: ProjectId validation is done at the endpoint level since it comes from the route
        // VariableSetId validation would require checking against the database
    }
}
