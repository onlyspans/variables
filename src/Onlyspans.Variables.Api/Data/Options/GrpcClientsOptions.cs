using Strongly.Options;

namespace Onlyspans.Variables.Api.Data.Options;

[StronglyOptions(SectionName)]
public sealed class GrpcClientsOptions
{
    public const string SectionName = "GrpcClients";

    public required string ProjectsServiceUrl { get; init; }
    public required string TargetsPlaneServiceUrl { get; init; }
}
