using Mediator;

namespace Onlyspans.Variables.Api.Features.VariableSets;

public record DeleteVariableSet(Guid Id) : ICommand;

public class DeleteVariableSetHandler : ICommandHandler<DeleteVariableSet>
{
    public ValueTask<Unit> Handle(DeleteVariableSet command, CancellationToken cancellationToken)
    {
        throw new NotImplementedException("Phase 5: Implement DeleteVariableSetHandler");
    }
}
