using Microsoft.Extensions.Options;
using Onlyspans.Variables.Api.Data.Options;
using Onlyspans.Variables.Api.Services;

namespace Onlyspans.Variables.Api.Startup;

public static partial class Startup
{
    private static WebApplicationBuilder AddGrpcServices(
        this WebApplicationBuilder builder)
    {
        builder.Services.AddGrpc();

        if (builder.Environment.IsDevelopment())
            builder.Services.AddGrpcReflection();

        builder.Services.AddGrpcClient<Projects.V1.ProjectsService.ProjectsServiceClient>((provider, options) =>
        {
            options.Address = new Uri(provider.GetRequiredService<IOptions<GrpcClientsOptions>>().Value.ProjectsServiceUrl);
        });

        builder.Services.AddTransient<Abstractions.Services.IProjectsClient, GrpcProjectsClient>();

        return builder;
    }

    private static WebApplication UseGrpcServices(this WebApplication app)
    {
        app.MapGrpcService<gRPC.Services.VariablesGrpcService>();

        if (app.Environment.IsDevelopment())
        {
            app.MapGrpcReflectionService();
        }

        return app;
    }
}
