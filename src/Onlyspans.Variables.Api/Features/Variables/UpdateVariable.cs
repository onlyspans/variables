using Mediator;
using Microsoft.EntityFrameworkCore;
using Onlyspans.Variables.Api.Data.Contexts;
using Onlyspans.Variables.Api.Data.Records;

namespace Onlyspans.Variables.Api.Features.Variables;

public record UpdateVariable(Guid Id, UpdateVariableRequest Request) : ICommand<VariableResponse>;

public sealed class UpdateVariableHandler(
    ApplicationDbContext db,
    ILogger<UpdateVariableHandler> logger)
    : ICommandHandler<UpdateVariable, VariableResponse>
{
    public async ValueTask<VariableResponse> Handle(UpdateVariable command, CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating variable {VariableId}", command.Id);

        var variable = await db.Variables
            .FirstOrDefaultAsync(v => v.Id == command.Id, cancellationToken);

        if (variable is null)
        {
            throw new InvalidOperationException($"Variable {command.Id} not found");
        }

        if (command.Request.Key is not null)
        {
            variable.Key = command.Request.Key;
        }

        if (command.Request.Value is not null)
        {
            variable.Value = command.Request.Value;
        }

        if (command.Request.EnvironmentId.HasValue)
        {
            variable.EnvironmentId = command.Request.EnvironmentId;
        }

        variable.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Updated variable {VariableId}", variable.Id);

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
