using Mediator;
using Onlyspans.Variables.Api.Data.Records;

namespace Onlyspans.Variables.Api.Features.Variables;

public record CreateVariable(Guid ProjectId, CreateVariableRequest Request) : ICommand<VariableResponse>;

public class CreateVariableHandler : ICommandHandler<CreateVariable, VariableResponse>
{
    public ValueTask<VariableResponse> Handle(CreateVariable command, CancellationToken cancellationToken)
    {
        throw new NotImplementedException("Phase 5: Implement CreateVariableHandler");
    }
}
