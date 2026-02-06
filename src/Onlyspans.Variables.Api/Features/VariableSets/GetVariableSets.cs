using Mediator;
using Microsoft.EntityFrameworkCore;
using Onlyspans.Variables.Api.Data.Contexts;
using Onlyspans.Variables.Api.Data.Records;

namespace Onlyspans.Variables.Api.Features.VariableSets;

public record GetVariableSets : IQuery<List<VariableSetResponse>>;

public sealed class GetVariableSetsHandler(
    ApplicationDbContext db,
    ILogger<GetVariableSetsHandler> logger)
    : IQueryHandler<GetVariableSets, List<VariableSetResponse>>
{
    public async ValueTask<List<VariableSetResponse>> Handle(GetVariableSets query, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting all variable sets");

        var variableSets = await db.VariableSets.ToListAsync(cancellationToken);

        return variableSets.Select(vs => new VariableSetResponse(
            vs.Id,
            vs.Name,
            vs.Description,
            vs.CreatedAt,
            vs.UpdatedAt
        )).ToList();
    }
}
