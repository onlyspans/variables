using FluentValidation;
using Onlyspans.Variables.Api.Abstractions.Services;
using Onlyspans.Variables.Api.Data.Records;

namespace Onlyspans.Variables.Api.Data.Validators;

public sealed class UpdateVariableRequestValidator : AbstractValidator<UpdateVariableRequest>
{
    public UpdateVariableRequestValidator(ITargetsPlaneClient targetsPlaneClient)
    {
        RuleFor(x => x.Key)
            .MaximumLength(256)
            .WithMessage("Variable key must not exceed 256 characters")
            .Matches("^[a-zA-Z0-9_.-]+$")
            .WithMessage("Variable key must contain only alphanumeric characters, underscores, dots, and hyphens")
            .When(x => !string.IsNullOrEmpty(x.Key));

        // Value can be null to indicate no change, but if provided it should not be null
        // This is a partial update, so all fields are optional

        RuleFor(x => x.EnvironmentId)
            .MustAsync(async (environmentId, ct) =>
            {
                if (!environmentId.HasValue)
                    return true; // Unscoped variables are allowed

                return await targetsPlaneClient.EnvironmentExistsAsync(environmentId.Value, ct);
            })
            .WithMessage("Environment does not exist")
            .When(x => x.EnvironmentId.HasValue);

        // At least one field must be provided for an update
        RuleFor(x => x)
            .Must(x => x.Key != null || x.Value != null || x.EnvironmentId != null)
            .WithMessage("At least one field must be provided for update");
    }
}
