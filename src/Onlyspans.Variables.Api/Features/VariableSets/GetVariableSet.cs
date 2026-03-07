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

        var variableSet = await db
            .VariableSets
            .Where(x => x.Id == query.Id)
            .Select(x => new VariableSetDetailResponse(
                x.Id, x.Name, x.Description,
                x.Variables.Select(v => new VariableResponse(v.Id, v.Key, v.Value, v.EnvironmentId, v.ProjectId, v.VariableSetId, v.CreatedAt, v.UpdatedAt)).ToList(),
                x.CreatedAt, x.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (variableSet is null)
            throw new InvalidOperationException($"Variable set {query.Id} not found");

        return variableSet;
    }
}
