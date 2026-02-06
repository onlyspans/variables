using Onlyspans.Variables.Api.Abstractions.Services;

namespace Onlyspans.Variables.Api.Services;

/// <summary>
/// Stub implementation of ITargetsPlaneClient for development
/// TODO: Replace with actual gRPC client implementation when TargetsPlane service is available
/// </summary>
public sealed class StubTargetsPlaneClient(ILogger<StubTargetsPlaneClient> logger) : ITargetsPlaneClient
{
    public Task<bool> EnvironmentExistsAsync(Guid environmentId, CancellationToken ct)
    {
        logger.LogWarning(
            "StubTargetsPlaneClient.EnvironmentExistsAsync called for environment {EnvironmentId} - returning true (stub)",
            environmentId);

        // For development: always return true
        // In production: this should call the actual TargetsPlane service via gRPC
        return Task.FromResult(true);
    }
}
