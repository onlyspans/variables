namespace Onlyspans.Variables.Api.Data.Entities;

public class VariableSet
{
    public Guid Id { get; init; }
    public required string Name { get; set; }
    public string? Description { get; set; }

    public virtual ICollection<Variable> Variables { get; init; } = [];

    public required DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; set; }
}
