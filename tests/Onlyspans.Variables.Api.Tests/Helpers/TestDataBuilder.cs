using Onlyspans.Variables.Api.Data.Entities;

namespace Onlyspans.Variables.Api.Tests.Helpers;

public static class TestDataBuilder
{
    public static Variable CreateVariable(
        Guid? id = null,
        string? key = null,
        string? value = null,
        Guid? environmentId = null,
        Guid? projectId = null,
        Guid? variableSetId = null,
        DateTime? createdAt = null,
        DateTime? updatedAt = null)
    {
        var now = DateTime.UtcNow;
        return new Variable
        {
            Id = id ?? Guid.NewGuid(),
            Key = key ?? "TEST_KEY",
            Value = value ?? "TEST_VALUE",
            EnvironmentId = environmentId,
            ProjectId = projectId,
            VariableSetId = variableSetId,
            CreatedAt = createdAt ?? now,
            UpdatedAt = updatedAt ?? now
        };
    }

    public static VariableSet CreateVariableSet(
        Guid? id = null,
        string? name = null,
        string? description = null,
        DateTime? createdAt = null,
        DateTime? updatedAt = null)
    {
        var now = DateTime.UtcNow;
        return new VariableSet
        {
            Id = id ?? Guid.NewGuid(),
            Name = name ?? "Test Set",
            Description = description,
            CreatedAt = createdAt ?? now,
            UpdatedAt = updatedAt ?? now
        };
    }

    public static ProjectVariableSetLink CreateLink(
        Guid projectId,
        Guid variableSetId,
        DateTime? linkedAt = null)
    {
        return new ProjectVariableSetLink
        {
            ProjectId = projectId,
            VariableSetId = variableSetId,
            LinkedAt = linkedAt ?? DateTime.UtcNow
        };
    }
}
