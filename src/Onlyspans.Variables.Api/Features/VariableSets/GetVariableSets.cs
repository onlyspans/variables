using Mediator;
using Onlyspans.Variables.Api.Data.Records;

namespace Onlyspans.Variables.Api.Features.VariableSets;

public record GetVariableSets : IQuery<List<VariableSetResponse>>;

public class GetVariableSetsHandler : IQueryHandler<GetVariableSets, List<VariableSetResponse>>
{
    public ValueTask<List<VariableSetResponse>> Handle(GetVariableSets query, CancellationToken cancellationToken)
    {
        throw new NotImplementedException("Phase 5: Implement GetVariableSetsHandler");
    }
}
