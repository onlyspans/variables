namespace Onlyspans.Variables.Api.Data.Records;

public record CreateVariableSetRequest(
    string Name,
    string? Description = null
);

public record UpdateVariableSetRequest(
    string? Name = null,
    string? Description = null
);

public record VariableSetResponse(
    Guid Id,
    string Name,
    string? Description,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record VariableSetDetailResponse(
    Guid Id,
    string Name,
    string? Description,
    List<VariableResponse> Variables,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
