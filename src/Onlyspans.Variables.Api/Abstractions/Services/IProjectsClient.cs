namespace Onlyspans.Variables.Api.Abstractions.Services;

/// <summary>
/// Client for calling the Projects service to validate project existence
/// </summary>
public interface IProjectsClient
{
    /// <summary>
    /// Validates whether a project with the given ID exists
    /// </summary>
    /// <param name="projectId">The project ID to validate</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if the project exists, false otherwise</returns>
    Task<bool> ProjectExistsAsync(Guid projectId, CancellationToken ct);
}
