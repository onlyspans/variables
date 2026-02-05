using Mediator;
using Onlyspans.Variables.Api.Data.Records;

namespace Onlyspans.Variables.Api.Features.VariableSets;

public record AddVariableToSet(Guid SetId, CreateVariableRequest Request) : ICommand<VariableResponse>;

public class AddVariableToSetHandler : ICommandHandler<AddVariableToSet, VariableResponse>
{
    public ValueTask<VariableResponse> Handle(AddVariableToSet command, CancellationToken cancellationToken)
    {
        throw new NotImplementedException("Phase 5: Implement AddVariableToSetHandler");
    }
}
