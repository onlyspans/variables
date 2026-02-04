namespace Onlyspans.Variables.Api.Data.Entities;

/// <summary>
/// Links a project to a variable set. Projects are external entities,
/// so we only store their IDs without a navigation property.
/// </summary>
public class ProjectVariableSetLink
{
    public Guid ProjectId { get; init; }
    public Guid VariableSetId { get; init; }

    public virtual VariableSet VariableSet { get; init; } = null!;

    public required DateTime LinkedAt { get; init; }
}
