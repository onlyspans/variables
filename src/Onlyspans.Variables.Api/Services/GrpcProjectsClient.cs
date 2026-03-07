using Grpc.Core;
using Onlyspans.Variables.Api.Abstractions.Services;
using Projects.V1;

namespace Onlyspans.Variables.Api.Services;

public sealed class GrpcProjectsClient(
    ProjectsService.ProjectsServiceClient client,
    ILogger<GrpcProjectsClient> logger) : IProjectsClient
{
    public async Task<bool> ProjectExistsAsync(Guid projectId, CancellationToken ct)
    {
        try
        {
            var response = await client.ProjectExistsAsync(
                new ProjectExistsRequest { Id = projectId.ToString() },
                cancellationToken: ct);
            return response.Exists;
        }
        catch (RpcException ex)
        {
            logger.LogError(ex, "gRPC error checking project {ProjectId} existence", projectId);
            throw new InvalidOperationException($"Failed to verify project {projectId}", ex);
        }
    }
}
