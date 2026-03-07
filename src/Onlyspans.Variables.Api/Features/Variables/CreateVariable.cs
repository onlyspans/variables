using Mediator;
using Onlyspans.Variables.Api.Data.Contexts;
using Onlyspans.Variables.Api.Data.Entities;
using Onlyspans.Variables.Api.Data.Records;

namespace Onlyspans.Variables.Api.Features.Variables;

public record CreateVariable(Guid ProjectId, CreateVariableRequest Request) : ICommand<VariableResponse>;

public sealed class CreateVariableHandler(
    ApplicationDbContext db,
    ILogger<CreateVariableHandler> logger)
    : ICommandHandler<CreateVariable, VariableResponse>
{
    public async ValueTask<VariableResponse> Handle(CreateVariable command, CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating variable {Key} for project {ProjectId}",
            command.Request.Key, command.ProjectId);

        var now = DateTime.UtcNow;
        var variable = new Variable
        {
            Key = command.Request.Key,
            Value = command.Request.Value,
            EnvironmentId = command.Request.EnvironmentId,
            ProjectId = command.ProjectId,
            VariableSetId = command.Request.VariableSetId,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Variables.Add(variable);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Created variable {VariableId} with key {Key}", variable.Id, variable.Key);

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
