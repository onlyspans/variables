using Mediator;
using Onlyspans.Variables.Api.Data.Records;

namespace Onlyspans.Variables.Api.Features.VariableSets;

public record CreateVariableSet(CreateVariableSetRequest Request) : ICommand<VariableSetResponse>;

public class CreateVariableSetHandler : ICommandHandler<CreateVariableSet, VariableSetResponse>
{
    public ValueTask<VariableSetResponse> Handle(CreateVariableSet command, CancellationToken cancellationToken)
    {
        throw new NotImplementedException("Phase 5: Implement CreateVariableSetHandler");
    }
}
