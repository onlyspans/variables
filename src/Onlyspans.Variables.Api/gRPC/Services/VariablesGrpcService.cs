using Grpc.Core;
using Mediator;
using Onlyspans.Variables.Api.Abstractions.Services;
using Onlyspans.Variables.Api.Data.Exceptions;
using Variables.Communication;

namespace Onlyspans.Variables.Api.gRPC.Services;

public sealed class VariablesGrpcService(
    ISender sender,
    IProjectsClient projectsClient,
    ILogger<VariablesGrpcService> logger)
    : VariablesService.VariablesServiceBase
{
    public override async Task<GetResolvedVariablesResult> GetResolvedVariables(
        GetResolvedVariablesInput request,
        ServerCallContext context)
    {
        logger.LogInformation(
            "GetResolvedVariables gRPC call for project {ProjectId}, environment {EnvironmentId}",
            request.ProjectId,
            request.EnvironmentId);

        try
        {
            if (!Guid.TryParse(request.ProjectId, out var projectId))
            {
                logger.LogWarning("Invalid project ID format: {ProjectId}", request.ProjectId);
                return new GetResolvedVariablesResult
                {
                    InternalError = new InternalError
                    {
                        Message = "Invalid project ID format",
                        Trace = string.Empty
                    }
                };
            }

            if (!Guid.TryParse(request.EnvironmentId, out var environmentId))
            {
                logger.LogWarning("Invalid environment ID format: {EnvironmentId}", request.EnvironmentId);
                return new GetResolvedVariablesResult
                {
                    InternalError = new InternalError
                    {
                        Message = "Invalid environment ID format",
                        Trace = string.Empty
                    }
                };
            }

            var query = new Features.Variables.GetResolvedVariables(projectId, environmentId);
            var result = await sender.Send(query, context.CancellationToken);

            var success = new GetResolvedVariablesResult.Types.Success();
            success.Variables.AddRange(result.Select(v => new Variable
            {
                Key = v.Key,
                Value = v.Value,
                EnvironmentId = v.EnvironmentId?.ToString(),
                Source = v.ProjectId.HasValue ? "project" : "variable_set"
            }));

            logger.LogInformation(
                "GetResolvedVariables completed successfully with {Count} variables",
                result.Count);

            return new GetResolvedVariablesResult
            {
                Success = success
            };
        }
        catch (VariableConflictException ex)
        {
            logger.LogWarning(
                "Variable conflict detected for key {Key}: {Sources}",
                ex.Key,
                string.Join(", ", ex.Sources));

            var conflictError = new GetResolvedVariablesResult.Types.ConflictError
            {
                Key = ex.Key
            };
            conflictError.ConflictingSources.AddRange(ex.Sources);

            return new GetResolvedVariablesResult
            {
                ConflictError = conflictError
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetResolvedVariables gRPC call");
            return new GetResolvedVariablesResult
            {
                InternalError = new InternalError
                {
                    Message = ex.Message,
                    Trace = ex.StackTrace ?? string.Empty
                }
            };
        }
    }

    public override async Task<ValidationResult> ValidateProjectExists(
        ValidateProjectInput request,
        ServerCallContext context)
    {
        logger.LogInformation("ValidateProjectExists gRPC call for project {ProjectId}", request.ProjectId);

        try
        {
            if (!Guid.TryParse(request.ProjectId, out var projectId))
            {
                return new ValidationResult
                {
                    Exists = false,
                    ErrorMessage = "Invalid project ID format"
                };
            }

            var exists = await projectsClient.ProjectExistsAsync(projectId, context.CancellationToken);

            return new ValidationResult
            {
                Exists = exists,
                ErrorMessage = exists ? string.Empty : "Project not found"
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating project existence");
            return new ValidationResult
            {
                Exists = false,
                ErrorMessage = $"Validation error: {ex.Message}"
            };
        }
    }

}
