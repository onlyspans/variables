using Mediator;
using Microsoft.EntityFrameworkCore;
using Onlyspans.Variables.Api.Data.Contexts;
using Onlyspans.Variables.Api.Data.Records;

namespace Onlyspans.Variables.Api.Features.Variables;

public record GetProjectVariables(Guid ProjectId) : IQuery<List<VariableResponse>>;

public sealed class GetProjectVariablesHandler(
    ApplicationDbContext db,
    ILogger<GetProjectVariablesHandler> logger)
    : IQueryHandler<GetProjectVariables, List<VariableResponse>>
{
    public async ValueTask<List<VariableResponse>> Handle(GetProjectVariables query, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting direct project variables for project {ProjectId}", query.ProjectId);

        var variables = await db.Variables
            .Where(v => v.ProjectId == query.ProjectId)
            .ToListAsync(cancellationToken);

        return variables.Select(v => new VariableResponse(
            v.Id,
            v.Key,
            v.Value,
            v.EnvironmentId,
            v.ProjectId,
            v.VariableSetId,
            v.CreatedAt,
            v.UpdatedAt
        )).ToList();
    }
}
