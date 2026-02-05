using Mediator;
using Onlyspans.Variables.Api.Data.Records;

namespace Onlyspans.Variables.Api.Features.Variables;

public record GetProjectVariables(Guid ProjectId) : IQuery<List<VariableResponse>>;

public class GetProjectVariablesHandler : IQueryHandler<GetProjectVariables, List<VariableResponse>>
{
    public ValueTask<List<VariableResponse>> Handle(GetProjectVariables query, CancellationToken cancellationToken)
    {
        throw new NotImplementedException("Phase 5: Implement GetProjectVariablesHandler");
    }
}
