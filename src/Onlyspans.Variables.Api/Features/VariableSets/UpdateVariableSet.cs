using Mediator;
using Microsoft.EntityFrameworkCore;
using Onlyspans.Variables.Api.Data.Contexts;
using Onlyspans.Variables.Api.Data.Records;

namespace Onlyspans.Variables.Api.Features.VariableSets;

public record UpdateVariableSet(Guid Id, UpdateVariableSetRequest Request) : ICommand<VariableSetResponse>;

public sealed class UpdateVariableSetHandler(
    ApplicationDbContext db,
    ILogger<UpdateVariableSetHandler> logger)
    : ICommandHandler<UpdateVariableSet, VariableSetResponse>
{
    public async ValueTask<VariableSetResponse> Handle(UpdateVariableSet command, CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating variable set {VariableSetId}", command.Id);

        var variableSet = await db.VariableSets
            .FirstOrDefaultAsync(vs => vs.Id == command.Id, cancellationToken);

        if (variableSet is null)
        {
            throw new InvalidOperationException($"Variable set {command.Id} not found");
        }

        if (command.Request.Name is not null)
        {
            variableSet.Name = command.Request.Name;
        }

        if (command.Request.Description is not null)
        {
            variableSet.Description = command.Request.Description;
        }

        variableSet.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Updated variable set {VariableSetId}", variableSet.Id);

        return new VariableSetResponse(
            variableSet.Id,
            variableSet.Name,
            variableSet.Description,
            variableSet.CreatedAt,
            variableSet.UpdatedAt
        );
    }
}
