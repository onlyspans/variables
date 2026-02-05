namespace Onlyspans.Variables.Api.Data.Records;

public record CreateVariableRequest(
    string Key,
    string Value,
    Guid? EnvironmentId = null,
    Guid? VariableSetId = null
);

public record UpdateVariableRequest(
    string? Key = null,
    string? Value = null,
    Guid? EnvironmentId = null
);

public record VariableResponse(
    Guid Id,
    string Key,
    string Value,
    Guid? EnvironmentId,
    Guid? ProjectId,
    Guid? VariableSetId,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
