using Mediator;
using Microsoft.EntityFrameworkCore;
using Onlyspans.Variables.Api.Data.Contexts;
using Onlyspans.Variables.Api.Data.Records;

namespace Onlyspans.Variables.Api.Features.VariableSets;

public record GetVariableSet(Guid Id) : IQuery<VariableSetDetailResponse>;

public sealed class GetVariableSetHandler(
    ApplicationDbContext db,
    ILogger<GetVariableSetHandler> logger)
    : IQueryHandler<GetVariableSet, VariableSetDetailResponse>
{
    public async ValueTask<VariableSetDetailResponse> Handle(GetVariableSet query, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting variable set {VariableSetId} with variables", query.Id);

        var variableSet = await db.VariableSets
            .Include(vs => vs.Variables)
            .FirstOrDefaultAsync(vs => vs.Id == query.Id, cancellationToken);

        if (variableSet is null)
        {
            throw new InvalidOperationException($"Variable set {query.Id} not found");
        }

        var variables = variableSet.Variables.Select(v => new VariableResponse(
            v.Id,
            v.Key,
            v.Value,
            v.EnvironmentId,
            v.ProjectId,
            v.VariableSetId,
            v.CreatedAt,
            v.UpdatedAt
        )).ToList();

        return new VariableSetDetailResponse(
            variableSet.Id,
            variableSet.Name,
            variableSet.Description,
            variables,
            variableSet.CreatedAt,
            variableSet.UpdatedAt
        );
    }
}
