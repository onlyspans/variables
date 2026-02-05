using Mediator;

namespace Onlyspans.Variables.Api.Features.Variables;

public record DeleteVariable(Guid Id) : ICommand;

public class DeleteVariableHandler : ICommandHandler<DeleteVariable>
{
    public ValueTask<Unit> Handle(DeleteVariable command, CancellationToken cancellationToken)
    {
        throw new NotImplementedException("Phase 5: Implement DeleteVariableHandler");
    }
}
