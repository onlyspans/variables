using FluentValidation;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
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

    private static async Task<Ok<List<VariableSetResponse>>> GetVariableSets(
        [FromServices] ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetVariableSets(), ct);
        return TypedResults.Ok(result);
    }

    private static async Task<Ok<VariableSetDetailResponse>> GetVariableSet(
        [FromRoute] Guid id,
        [FromServices] ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetVariableSet(id), ct);
        return TypedResults.Ok(result);
    }

    private static async Task<Results<Created<VariableSetResponse>, ValidationProblem>> CreateVariableSet(
        [FromBody] CreateVariableSetRequest request,
        [FromServices] ISender sender,
        [FromServices] IValidator<CreateVariableSetRequest> validator,
        CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        var result = await sender.Send(new CreateVariableSet(request), ct);
        return TypedResults.Created($"/api/variable-sets/{result.Id}", result);
    }

    private static async Task<Results<Ok<VariableSetResponse>, ValidationProblem>> UpdateVariableSet(
        [FromRoute] Guid id,
        [FromBody] UpdateVariableSetRequest request,
        [FromServices] ISender sender,
        [FromServices] IValidator<UpdateVariableSetRequest> validator,
        CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        var result = await sender.Send(new UpdateVariableSet(id, request), ct);
        return TypedResults.Ok(result);
    }

    private static async Task<NoContent> DeleteVariableSet(
        [FromRoute] Guid id,
        [FromServices] ISender sender,
        CancellationToken ct)
    {
        await sender.Send(new DeleteVariableSet(id), ct);
        return TypedResults.NoContent();
    }

    private static async Task<Results<Created<VariableResponse>, ValidationProblem>> AddVariableToSet(
        [FromRoute] Guid id,
        [FromBody] CreateVariableRequest request,
        [FromServices] ISender sender,
        [FromServices] IValidator<CreateVariableRequest> validator,
        CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        var result = await sender.Send(new AddVariableToSet(id, request), ct);
        return TypedResults.Created($"/api/variables/{result.Id}", result);
    }
}
