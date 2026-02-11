using Mediator;
using Microsoft.EntityFrameworkCore;
using Onlyspans.Variables.Api.Data.Contexts;
using Onlyspans.Variables.Api.Data.Entities;
using Onlyspans.Variables.Api.Data.Records;

namespace Onlyspans.Variables.Api.Features.VariableSets;

public record AddVariableToSet(Guid SetId, CreateVariableRequest Request) : ICommand<VariableResponse>;

public sealed class AddVariableToSetHandler(
    ApplicationDbContext db,
    ILogger<AddVariableToSetHandler> logger)
    : ICommandHandler<AddVariableToSet, VariableResponse>
{
    public async ValueTask<VariableResponse> Handle(AddVariableToSet command, CancellationToken cancellationToken)
    {
        logger.LogInformation("Adding variable {Key} to variable set {SetId}",
            command.Request.Key, command.SetId);

        // Validate variable set exists
        var setExists = await db.VariableSets.AnyAsync(vs => vs.Id == command.SetId, cancellationToken);
        if (!setExists)
        {
            logger.LogWarning("Variable set {SetId} does not exist", command.SetId);
            throw new InvalidOperationException($"Variable set {command.SetId} does not exist");
        }

        var now = DateTime.UtcNow;
        var variable = new Variable
        {
            Id = Guid.NewGuid(),
            Key = command.Request.Key,
            Value = command.Request.Value,
            EnvironmentId = command.Request.EnvironmentId,
            ProjectId = null,
            VariableSetId = command.SetId,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Variables.Add(variable);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Added variable {VariableId} with key {Key} to set {SetId}",
            variable.Id, variable.Key, command.SetId);

        return new VariableResponse(
            variable.Id,
            variable.Key,
            variable.Value,
            variable.EnvironmentId,
            variable.ProjectId,
            variable.VariableSetId,
            variable.CreatedAt,
            variable.UpdatedAt
        );
    }
}
