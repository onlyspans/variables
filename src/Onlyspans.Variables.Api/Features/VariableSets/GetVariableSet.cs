using Mediator;
using Onlyspans.Variables.Api.Data.Records;

namespace Onlyspans.Variables.Api.Features.VariableSets;

public record GetVariableSet(Guid Id) : IQuery<VariableSetDetailResponse>;

public class GetVariableSetHandler : IQueryHandler<GetVariableSet, VariableSetDetailResponse>
{
    public ValueTask<VariableSetDetailResponse> Handle(GetVariableSet query, CancellationToken cancellationToken)
    {
        throw new NotImplementedException("Phase 5: Implement GetVariableSetHandler");
    }
}
