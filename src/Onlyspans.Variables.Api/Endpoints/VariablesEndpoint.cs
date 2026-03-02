using FluentValidation;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Onlyspans.Variables.Api.Abstractions.Services;
using Onlyspans.Variables.Api.Data.Records;
using Onlyspans.Variables.Api.Features.Variables;

namespace Onlyspans.Variables.Api.Endpoints;

public static class VariablesEndpoint
{
    public static WebApplication MapVariablesEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/projects/{projectId:guid}/variables")
            .WithTags("Variables");

        group.MapGet("", GetProjectVariables);
        group.MapPost("", CreateVariable);

        app.MapPut("/api/variables/{id:guid}", UpdateVariable)
            .WithTags("Variables");

        app.MapDelete("/api/variables/{id:guid}", DeleteVariable)
            .WithTags("Variables");

        return app;
    }

    private static async Task<IResult> GetProjectVariables(
        [FromRoute] Guid projectId,
        [FromServices] ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetProjectVariables(projectId), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> CreateVariable(
        [FromRoute] Guid projectId,
        [FromBody] CreateVariableRequest request,
        [FromServices] ISender sender,
        [FromServices] IValidator<CreateVariableRequest> validator,
        [FromServices] IProjectsClient projectsClient,
        CancellationToken ct)
    {
        if (!await projectsClient.ProjectExistsAsync(projectId, ct))
            return Results.NotFound($"Project {projectId} not found");

        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var result = await sender.Send(new CreateVariable(projectId, request), ct);
        return Results.Created($"/api/variables/{result.Id}", result);
    }

    private static async Task<IResult> UpdateVariable(
        [FromRoute] Guid id,
        [FromBody] UpdateVariableRequest request,
        [FromServices] ISender sender,
        [FromServices] IValidator<UpdateVariableRequest> validator,
        CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var result = await sender.Send(new UpdateVariable(id, request), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> DeleteVariable(
        [FromRoute] Guid id,
        [FromServices] ISender sender,
        CancellationToken ct)
    {
        await sender.Send(new DeleteVariable(id), ct);
        return Results.NoContent();
    }
}
