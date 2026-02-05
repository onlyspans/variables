using Mediator;
using Onlyspans.Variables.Api.Data.Records;

namespace Onlyspans.Variables.Api.Features.Variables;

public record UpdateVariable(Guid Id, UpdateVariableRequest Request) : ICommand<VariableResponse>;

public class UpdateVariableHandler : ICommandHandler<UpdateVariable, VariableResponse>
{
    public ValueTask<VariableResponse> Handle(UpdateVariable command, CancellationToken cancellationToken)
    {
        throw new NotImplementedException("Phase 5: Implement UpdateVariableHandler");
    }
}
