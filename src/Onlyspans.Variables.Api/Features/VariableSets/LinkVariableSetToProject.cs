using Mediator;
using Microsoft.EntityFrameworkCore;
using Onlyspans.Variables.Api.Data.Contexts;
using Onlyspans.Variables.Api.Data.Entities;

namespace Onlyspans.Variables.Api.Features.VariableSets;

public record LinkVariableSetToProject(Guid ProjectId, Guid SetId) : ICommand;

public sealed class LinkVariableSetToProjectHandler(
    ApplicationDbContext db,
    ILogger<LinkVariableSetToProjectHandler> logger)
    : ICommandHandler<LinkVariableSetToProject>
{
    public async ValueTask<Unit> Handle(LinkVariableSetToProject command, CancellationToken cancellationToken)
    {
        logger.LogInformation("Linking variable set {SetId} to project {ProjectId}",
            command.SetId, command.ProjectId);

        // Check if link already exists
        var existingLink = await db.ProjectVariableSetLinks
            .FirstOrDefaultAsync(l => l.ProjectId == command.ProjectId && l.VariableSetId == command.SetId,
                cancellationToken);

        if (existingLink is not null)
        {
            logger.LogWarning("Link between project {ProjectId} and set {SetId} already exists",
                command.ProjectId, command.SetId);
            return Unit.Value;
        }

        var link = new ProjectVariableSetLink
        {
            ProjectId = command.ProjectId,
            VariableSetId = command.SetId,
            LinkedAt = DateTime.UtcNow
        };

        db.ProjectVariableSetLinks.Add(link);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Linked variable set {SetId} to project {ProjectId}",
            command.SetId, command.ProjectId);

        return Unit.Value;
    }
}
