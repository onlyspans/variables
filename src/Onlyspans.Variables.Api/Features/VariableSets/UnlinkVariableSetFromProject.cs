using Mediator;
using Microsoft.EntityFrameworkCore;
using Onlyspans.Variables.Api.Data.Contexts;

namespace Onlyspans.Variables.Api.Features.VariableSets;

public record UnlinkVariableSetFromProject(Guid ProjectId, Guid SetId) : ICommand;

public sealed class UnlinkVariableSetFromProjectHandler(
    ApplicationDbContext db,
    ILogger<UnlinkVariableSetFromProjectHandler> logger)
    : ICommandHandler<UnlinkVariableSetFromProject>
{
    public async ValueTask<Unit> Handle(UnlinkVariableSetFromProject command, CancellationToken cancellationToken)
    {
        logger.LogInformation("Unlinking variable set {SetId} from project {ProjectId}",
            command.SetId, command.ProjectId);

        var link = await db.ProjectVariableSetLinks
            .FirstOrDefaultAsync(l => l.ProjectId == command.ProjectId && l.VariableSetId == command.SetId,
                cancellationToken);

        if (link is null)
        {
            // Idempotent behavior: if the link doesn't exist, the desired state is already achieved
            logger.LogInformation("Link between project {ProjectId} and set {SetId} not found (idempotent operation)",
                command.ProjectId, command.SetId);
            return Unit.Value;
        }

        db.ProjectVariableSetLinks.Remove(link);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Unlinked variable set {SetId} from project {ProjectId}",
            command.SetId, command.ProjectId);

        return Unit.Value;
    }
}
