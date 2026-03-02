using Onlyspans.Variables.Api.Abstractions.Services;
using Projects.V1;

namespace Onlyspans.Variables.Api.Services;

public sealed class GrpcProjectsClient(ProjectsService.ProjectsServiceClient client) : IProjectsClient
{
    public async Task<bool> ProjectExistsAsync(Guid projectId, CancellationToken ct)
    {
        var response = await client.ProjectExistsAsync(
            new ProjectExistsRequest { Id = projectId.ToString() },
            cancellationToken: ct);

        return response.Exists;
    }
}
