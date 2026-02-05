using Mediator;
using Onlyspans.Variables.Api.Data.Records;

namespace Onlyspans.Variables.Api.Features.VariableSets;

public record UpdateVariableSet(Guid Id, UpdateVariableSetRequest Request) : ICommand<VariableSetResponse>;

public class UpdateVariableSetHandler : ICommandHandler<UpdateVariableSet, VariableSetResponse>
{
    public ValueTask<VariableSetResponse> Handle(UpdateVariableSet command, CancellationToken cancellationToken)
    {
        throw new NotImplementedException("Phase 5: Implement UpdateVariableSetHandler");
    }
}
