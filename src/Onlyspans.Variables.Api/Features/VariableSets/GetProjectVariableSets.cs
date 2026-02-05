using Mediator;
using Onlyspans.Variables.Api.Data.Records;

namespace Onlyspans.Variables.Api.Features.VariableSets;

public record GetProjectVariableSets(Guid ProjectId) : IQuery<List<VariableSetResponse>>;

public class GetProjectVariableSetsHandler : IQueryHandler<GetProjectVariableSets, List<VariableSetResponse>>
{
    public ValueTask<List<VariableSetResponse>> Handle(GetProjectVariableSets query, CancellationToken cancellationToken)
    {
        throw new NotImplementedException("Phase 5: Implement GetProjectVariableSetsHandler");
    }
}
