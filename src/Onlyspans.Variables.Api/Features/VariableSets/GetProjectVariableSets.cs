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

        return await db
            .ProjectVariableSetLinks
            .Where(x => x.ProjectId == query.ProjectId)
            .Select(x => new VariableSetResponse(x.VariableSet.Id, x.VariableSet.Name, x.VariableSet.Description, x.VariableSet.CreatedAt, x.VariableSet.UpdatedAt))
            .ToListAsync(cancellationToken);
    }
}
