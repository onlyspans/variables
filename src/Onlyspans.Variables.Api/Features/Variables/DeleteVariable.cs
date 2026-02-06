using Mediator;
using Microsoft.EntityFrameworkCore;
using Onlyspans.Variables.Api.Data.Contexts;

namespace Onlyspans.Variables.Api.Features.Variables;

public record DeleteVariable(Guid Id) : ICommand;

public sealed class DeleteVariableHandler(
    ApplicationDbContext db,
    ILogger<DeleteVariableHandler> logger)
    : ICommandHandler<DeleteVariable>
{
    public async ValueTask<Unit> Handle(DeleteVariable command, CancellationToken cancellationToken)
    {
        logger.LogInformation("Deleting variable {VariableId}", command.Id);

        var variable = await db.Variables
            .FirstOrDefaultAsync(v => v.Id == command.Id, cancellationToken);

        if (variable is null)
        {
            throw new InvalidOperationException($"Variable {command.Id} not found");
        }

        db.Variables.Remove(variable);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Deleted variable {VariableId}", command.Id);

        return Unit.Value;
    }
}
