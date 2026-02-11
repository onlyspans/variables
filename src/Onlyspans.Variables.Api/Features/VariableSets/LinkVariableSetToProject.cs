using Mediator;
using Microsoft.EntityFrameworkCore;
using Onlyspans.Variables.Api.Abstractions.Services;
using Onlyspans.Variables.Api.Data.Contexts;
using Onlyspans.Variables.Api.Data.Entities;

namespace Onlyspans.Variables.Api.Features.VariableSets;

public record LinkVariableSetToProject(Guid ProjectId, Guid SetId) : ICommand;

public sealed class LinkVariableSetToProjectHandler(
    ApplicationDbContext db,
    IProjectsClient projectsClient,
    ILogger<LinkVariableSetToProjectHandler> logger)
    : ICommandHandler<LinkVariableSetToProject>
{
    public async ValueTask<Unit> Handle(LinkVariableSetToProject command, CancellationToken cancellationToken)
    {
        logger.LogInformation("Linking variable set {SetId} to project {ProjectId}",
            command.SetId, command.ProjectId);

        // Validate project exists
        var projectExists = await projectsClient.ProjectExistsAsync(command.ProjectId, cancellationToken);
        if (!projectExists)
        {
            logger.LogWarning("Project {ProjectId} does not exist", command.ProjectId);
            throw new InvalidOperationException($"Project {command.ProjectId} does not exist");
        }

        // Validate variable set exists
        var setExists = await db.VariableSets.AnyAsync(vs => vs.Id == command.SetId, cancellationToken);
        if (!setExists)
        {
            logger.LogWarning("Variable set {SetId} does not exist", command.SetId);
            throw new InvalidOperationException($"Variable set {command.SetId} does not exist");
        }

        // Check if link already exists
        var existingLink = await db.ProjectVariableSetLinks
            .FirstOrDefaultAsync(l => l.ProjectId == command.ProjectId && l.VariableSetId == command.SetId,
                cancellationToken);

        if (existingLink is not null)
        {
            logger.LogInformation("Link between project {ProjectId} and set {SetId} already exists, returning successfully",
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
