namespace Onlyspans.Variables.Api;

public static partial class Startup
{
    public static IServiceCollection AddGrpcServices(this IServiceCollection services)
    {
        services.AddGrpc();

        // Register gRPC clients for external service validation
        // TODO: Replace stub implementations with actual gRPC clients when services are available
        services.AddSingleton<Abstractions.Services.IProjectsClient, Services.StubProjectsClient>();
        services.AddSingleton<Abstractions.Services.ITargetsPlaneClient, Services.StubTargetsPlaneClient>();

        return services;
    }

    public static WebApplication UseGrpcServices(this WebApplication app)
    {
        app.MapGrpcService<gRPC.Services.VariablesGrpcService>();

        // Enable gRPC reflection in development for testing with grpcurl
        if (app.Environment.IsDevelopment())
        {
            app.MapGrpcReflectionService();
        }

        return app;
    }
}
