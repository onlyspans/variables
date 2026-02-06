using Mediator;
using Microsoft.EntityFrameworkCore;
using Onlyspans.Variables.Api.Data.Contexts;
using Onlyspans.Variables.Api.Data.Records;

namespace Onlyspans.Variables.Api.Features.VariableSets;

public record GetProjectVariableSets(Guid ProjectId) : IQuery<List<VariableSetResponse>>;

public sealed class GetProjectVariableSetsHandler(
    ApplicationDbContext db,
    ILogger<GetProjectVariableSetsHandler> logger)
    : IQueryHandler<GetProjectVariableSets, List<VariableSetResponse>>
{
    public async ValueTask<List<VariableSetResponse>> Handle(GetProjectVariableSets query, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting variable sets linked to project {ProjectId}", query.ProjectId);

        var variableSets = await (from link in db.ProjectVariableSetLinks
            join vs in db.VariableSets on link.VariableSetId equals vs.Id
            where link.ProjectId == query.ProjectId
            select vs).ToListAsync(cancellationToken);

        return variableSets.Select(vs => new VariableSetResponse(
            vs.Id,
            vs.Name,
            vs.Description,
            vs.CreatedAt,
            vs.UpdatedAt
        )).ToList();
    }
}
