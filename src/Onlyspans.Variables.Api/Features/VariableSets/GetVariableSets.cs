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

        return await db
            .VariableSets
            .Select(x => new VariableSetResponse(x.Id, x.Name, x.Description, x.CreatedAt, x.UpdatedAt))
            .ToListAsync(cancellationToken);
    }
}
