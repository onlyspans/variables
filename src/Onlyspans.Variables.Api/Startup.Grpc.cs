namespace Onlyspans.Variables.Api;

public static partial class Startup
{
    public static IServiceCollection AddGrpcServices(this IServiceCollection services)
    {
        services.AddGrpc();

        return services;
    }

    public static WebApplication UseGrpcServices(this WebApplication app)
    {
        // gRPC services will be mapped here in Phase 6
        return app;
    }
}
