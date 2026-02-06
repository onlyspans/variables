using FluentValidation;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Onlyspans.Variables.Api.Data.Records;
using Onlyspans.Variables.Api.Features.VariableSets;

namespace Onlyspans.Variables.Api.Endpoints;

public static class VariableSetsEndpoint
{
    public static WebApplication MapVariableSetsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/variable-sets")
            .WithTags("VariableSets");

        group.MapGet("", GetVariableSets);
        group.MapGet("{id:guid}", GetVariableSet);
        group.MapPost("", CreateVariableSet);
        group.MapPut("{id:guid}", UpdateVariableSet);
        group.MapDelete("{id:guid}", DeleteVariableSet);
        group.MapPost("{id:guid}/variables", AddVariableToSet);

        return app;
    }

    private static async Task<IResult> GetVariableSets(
        [FromServices] ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetVariableSets(), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetVariableSet(
        [FromRoute] Guid id,
        [FromServices] ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetVariableSet(id), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> CreateVariableSet(
        [FromBody] CreateVariableSetRequest request,
        [FromServices] ISender sender,
        [FromServices] IValidator<CreateVariableSetRequest> validator,
        CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var result = await sender.Send(new CreateVariableSet(request), ct);
        return Results.Created($"/api/variable-sets/{result.Id}", result);
    }

    private static async Task<IResult> UpdateVariableSet(
        [FromRoute] Guid id,
        [FromBody] UpdateVariableSetRequest request,
        [FromServices] ISender sender,
        [FromServices] IValidator<UpdateVariableSetRequest> validator,
        CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var result = await sender.Send(new UpdateVariableSet(id, request), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> DeleteVariableSet(
        [FromRoute] Guid id,
        [FromServices] ISender sender,
        CancellationToken ct)
    {
        await sender.Send(new DeleteVariableSet(id), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> AddVariableToSet(
        [FromRoute] Guid id,
        [FromBody] CreateVariableRequest request,
        [FromServices] ISender sender,
        [FromServices] IValidator<CreateVariableRequest> validator,
        CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var result = await sender.Send(new AddVariableToSet(id, request), ct);
        return Results.Created($"/api/variables/{result.Id}", result);
    }
}
