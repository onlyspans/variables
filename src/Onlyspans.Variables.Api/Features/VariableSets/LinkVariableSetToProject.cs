using Mediator;

namespace Onlyspans.Variables.Api.Features.VariableSets;

public record LinkVariableSetToProject(Guid ProjectId, Guid SetId) : ICommand;

public class LinkVariableSetToProjectHandler : ICommandHandler<LinkVariableSetToProject>
{
    public ValueTask<Unit> Handle(LinkVariableSetToProject command, CancellationToken cancellationToken)
    {
        throw new NotImplementedException("Phase 5: Implement LinkVariableSetToProjectHandler");
    }
}
