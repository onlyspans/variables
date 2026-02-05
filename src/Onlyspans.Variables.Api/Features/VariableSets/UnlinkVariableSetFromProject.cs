using Mediator;

namespace Onlyspans.Variables.Api.Features.VariableSets;

public record UnlinkVariableSetFromProject(Guid ProjectId, Guid SetId) : ICommand;

public class UnlinkVariableSetFromProjectHandler : ICommandHandler<UnlinkVariableSetFromProject>
{
    public ValueTask<Unit> Handle(UnlinkVariableSetFromProject command, CancellationToken cancellationToken)
    {
        throw new NotImplementedException("Phase 5: Implement UnlinkVariableSetFromProjectHandler");
    }
}
