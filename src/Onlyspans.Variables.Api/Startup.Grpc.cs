namespace Onlyspans.Variables.Api;

public static partial class Startup
{
    public static IServiceCollection AddGrpcServices(
        this IServiceCollection services,
        IHostEnvironment environment,
        Data.Options.GrpcClientsOptions grpcClientsOptions)
    {
        services.AddGrpc();

        if (environment.IsDevelopment())
        {
            services.AddGrpcReflection();
        }

        services.AddGrpcClient<Projects.V1.ProjectsService.ProjectsServiceClient>(options =>
        {
            options.Address = new Uri(grpcClientsOptions.ProjectsServiceUrl);
        });

        services.AddTransient<Abstractions.Services.IProjectsClient, Services.GrpcProjectsClient>();

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
