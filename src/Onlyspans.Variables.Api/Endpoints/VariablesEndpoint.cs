using FluentValidation;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
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

    private static async Task<Ok<List<VariableResponse>>> GetProjectVariables(
        [FromRoute] Guid projectId,
        [FromServices] ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetProjectVariables(projectId), ct);
        return TypedResults.Ok(result);
    }

    private static async Task<Results<Created<VariableResponse>, NotFound<string>, ValidationProblem>> CreateVariable(
        [FromRoute] Guid projectId,
        [FromBody] CreateVariableRequest request,
        [FromServices] ISender sender,
        [FromServices] IValidator<CreateVariableRequest> validator,
        [FromServices] IProjectsClient projectsClient,
        CancellationToken ct)
    {
        if (!await projectsClient.ProjectExistsAsync(projectId, ct))
            return TypedResults.NotFound($"Project {projectId} not found");

        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        var result = await sender.Send(new CreateVariable(projectId, request), ct);
        return TypedResults.Created($"/api/variables/{result.Id}", result);
    }

    private static async Task<Results<Ok<VariableResponse>, ValidationProblem>> UpdateVariable(
        [FromRoute] Guid id,
        [FromBody] UpdateVariableRequest request,
        [FromServices] ISender sender,
        [FromServices] IValidator<UpdateVariableRequest> validator,
        CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        var result = await sender.Send(new UpdateVariable(id, request), ct);
        return TypedResults.Ok(result);
    }

    private static async Task<NoContent> DeleteVariable(
        [FromRoute] Guid id,
        [FromServices] ISender sender,
        CancellationToken ct)
    {
        await sender.Send(new DeleteVariable(id), ct);
        return TypedResults.NoContent();
    }
}
