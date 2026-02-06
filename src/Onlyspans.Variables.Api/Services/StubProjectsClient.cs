using Onlyspans.Variables.Api.Abstractions.Services;

namespace Onlyspans.Variables.Api.Services;

/// <summary>
/// Stub implementation of IProjectsClient for development
/// TODO: Replace with actual gRPC client implementation when Projects service is available
/// </summary>
public sealed class StubProjectsClient(ILogger<StubProjectsClient> logger) : IProjectsClient
{
    public Task<bool> ProjectExistsAsync(Guid projectId, CancellationToken ct)
    {
        logger.LogWarning(
            "StubProjectsClient.ProjectExistsAsync called for project {ProjectId} - returning true (stub)",
            projectId);

        // For development: always return true
        // In production: this should call the actual Projects service via gRPC
        return Task.FromResult(true);
    }
}
