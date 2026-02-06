using Mediator;
using Microsoft.EntityFrameworkCore;
using Onlyspans.Variables.Api.Data.Contexts;
using Onlyspans.Variables.Api.Data.Entities;
using Onlyspans.Variables.Api.Data.Exceptions;
using Onlyspans.Variables.Api.Data.Records;

namespace Onlyspans.Variables.Api.Features.Variables;

public sealed record GetResolvedVariables(
    Guid ProjectId,
    Guid? EnvironmentId) : IQuery<List<VariableResponse>>;

public sealed class GetResolvedVariablesHandler(
    ApplicationDbContext db,
    ILogger<GetResolvedVariablesHandler> logger)
    : IQueryHandler<GetResolvedVariables, List<VariableResponse>>
{
    public async ValueTask<List<VariableResponse>> Handle(
        GetResolvedVariables query,
        CancellationToken ct)
    {
        logger.LogInformation(
            "Resolving variables for project {ProjectId}, environment {EnvironmentId}",
            query.ProjectId, query.EnvironmentId);

        // 1. Get all project variables
        var projectVars = await db.Variables
            .Where(v => v.ProjectId == query.ProjectId)
            .ToListAsync(ct);

        // 2. Get all linked variable sets for this project
        var linkedSetIds = await db.ProjectVariableSetLinks
            .Where(l => l.ProjectId == query.ProjectId)
            .Select(l => l.VariableSetId)
            .ToListAsync(ct);

        // 3. Get all variables from linked sets with set names
        var setVarsQuery = from v in db.Variables
                           join vs in db.VariableSets on v.VariableSetId equals vs.Id
                           where linkedSetIds.Contains(vs.Id)
                           select new { Variable = v, SetName = vs.Name };

        var setVars = await setVarsQuery.ToListAsync(ct);

        // 4. Apply scoping and conflict resolution
        var resolved = new Dictionary<string, (Variable Var, string Source)>();

        // Process variables by priority
        foreach (var sv in setVars)
        {
            var key = CreateKey(sv.Variable.Key, sv.Variable.EnvironmentId);

            if (resolved.ContainsKey(key))
            {
                // Conflict: multiple variable sets define the same key+scope
                var existing = resolved[key];
                if (existing.Source != "project")
                {
                    logger.LogWarning(
                        "Conflict detected for variable key {Key}: sources {Source1}, {Source2}",
                        sv.Variable.Key, existing.Source, sv.SetName);

                    throw new VariableConflictException(
                        sv.Variable.Key,
                        new List<string> { existing.Source, sv.SetName });
                }
            }
            else
            {
                resolved[key] = (sv.Variable, sv.SetName);
            }
        }

        // Project variables override variable set variables
        foreach (var pv in projectVars)
        {
            var key = CreateKey(pv.Key, pv.EnvironmentId);
            resolved[key] = (pv, "project");
        }

        // 5. Filter by environment scope if specified
        IEnumerable<(Variable Var, string Source)> results = resolved.Values;

        if (query.EnvironmentId.HasValue)
        {
            // Group by variable key, prefer environment-specific over unscoped
            results = results
                .GroupBy(r => r.Var.Key)
                .Select(g =>
                {
                    var envSpecific = g.FirstOrDefault(r => r.Var.EnvironmentId == query.EnvironmentId);
                    return envSpecific != default ? envSpecific : g.FirstOrDefault(r => r.Var.EnvironmentId == null);
                })
                .Where(r => r != default);
        }

        // 6. Map to response DTOs
        return results.Select(r => new VariableResponse(
            r.Var.Id,
            r.Var.Key,
            r.Var.Value,
            r.Var.EnvironmentId,
            r.Var.ProjectId,
            r.Var.VariableSetId,
            r.Var.CreatedAt,
            r.Var.UpdatedAt
        )).ToList();
    }

    private static string CreateKey(string key, Guid? envId) =>
        $"{key}:{envId?.ToString() ?? "unscoped"}";
}
