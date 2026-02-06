using Mediator;
using Microsoft.EntityFrameworkCore;
using Onlyspans.Variables.Api.Data.Contexts;

namespace Onlyspans.Variables.Api.Features.VariableSets;

public record DeleteVariableSet(Guid Id) : ICommand;

public sealed class DeleteVariableSetHandler(
    ApplicationDbContext db,
    ILogger<DeleteVariableSetHandler> logger)
    : ICommandHandler<DeleteVariableSet>
{
    public async ValueTask<Unit> Handle(DeleteVariableSet command, CancellationToken cancellationToken)
    {
        logger.LogInformation("Deleting variable set {VariableSetId}", command.Id);

        var variableSet = await db.VariableSets
            .FirstOrDefaultAsync(vs => vs.Id == command.Id, cancellationToken);

        if (variableSet is null)
        {
            throw new InvalidOperationException($"Variable set {command.Id} not found");
        }

        db.VariableSets.Remove(variableSet);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Deleted variable set {VariableSetId}", command.Id);

        return Unit.Value;
    }
}
