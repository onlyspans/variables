using Mediator;
using Microsoft.AspNetCore.Mvc;
using Onlyspans.Variables.Api.Features.VariableSets;

namespace Onlyspans.Variables.Api.Endpoints;

public static class ProjectVariableSetsEndpoint
{
    public static WebApplication MapProjectVariableSetsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/projects/{projectId:guid}/variable-sets")
            .WithTags("ProjectVariableSets");

        group.MapGet("", GetProjectVariableSets);
        group.MapPost("{setId:guid}", LinkVariableSetToProject);
        group.MapDelete("{setId:guid}", UnlinkVariableSetFromProject);

        return app;
    }

    private static async Task<IResult> GetProjectVariableSets(
        [FromRoute] Guid projectId,
        [FromServices] ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetProjectVariableSets(projectId), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> LinkVariableSetToProject(
        [FromRoute] Guid projectId,
        [FromRoute] Guid setId,
        [FromServices] ISender sender,
        CancellationToken ct)
    {
        await sender.Send(new LinkVariableSetToProject(projectId, setId), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> UnlinkVariableSetFromProject(
        [FromRoute] Guid projectId,
        [FromRoute] Guid setId,
        [FromServices] ISender sender,
        CancellationToken ct)
    {
        await sender.Send(new UnlinkVariableSetFromProject(projectId, setId), ct);
        return Results.NoContent();
    }
}
