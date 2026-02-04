namespace Onlyspans.Variables.Api.Data.Entities;

public class Variable
{
    public Guid Id { get; init; }
    public required string Key { get; set; }
    public required string Value { get; set; }
    public Guid? EnvironmentId { get; set; }  // Scope (nullable = no scope)

    // Belongs to either Project OR VariableSet (not both)
    public Guid? ProjectId { get; set; }
    public Guid? VariableSetId { get; set; }

    public virtual VariableSet? VariableSet { get; set; }

    public required DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; set; }
}
