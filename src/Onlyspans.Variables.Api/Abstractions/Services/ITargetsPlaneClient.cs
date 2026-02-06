namespace Onlyspans.Variables.Api.Abstractions.Services;

/// <summary>
/// Client for calling the TargetsPlane service to validate environment existence
/// </summary>
public interface ITargetsPlaneClient
{
    /// <summary>
    /// Validates whether an environment with the given ID exists
    /// </summary>
    /// <param name="environmentId">The environment ID to validate</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if the environment exists, false otherwise</returns>
    Task<bool> EnvironmentExistsAsync(Guid environmentId, CancellationToken ct);
}
