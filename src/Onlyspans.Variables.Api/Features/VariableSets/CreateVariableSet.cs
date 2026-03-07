using Mediator;
using Onlyspans.Variables.Api.Data.Contexts;
using Onlyspans.Variables.Api.Data.Entities;
using Onlyspans.Variables.Api.Data.Records;

namespace Onlyspans.Variables.Api.Features.VariableSets;

public record CreateVariableSet(CreateVariableSetRequest Request) : ICommand<VariableSetResponse>;

public sealed class CreateVariableSetHandler(
    ApplicationDbContext db,
    ILogger<CreateVariableSetHandler> logger)
    : ICommandHandler<CreateVariableSet, VariableSetResponse>
{
    public async ValueTask<VariableSetResponse> Handle(CreateVariableSet command, CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating variable set {Name}", command.Request.Name);

        var now = DateTime.UtcNow;
        var variableSet = new VariableSet
        {
            Name = command.Request.Name,
            Description = command.Request.Description,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.VariableSets.Add(variableSet);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Created variable set {VariableSetId} with name {Name}", variableSet.Id, variableSet.Name);

        return new VariableSetResponse(
            variableSet.Id,
            variableSet.Name,
            variableSet.Description,
            variableSet.CreatedAt,
            variableSet.UpdatedAt
        );
    }
}
