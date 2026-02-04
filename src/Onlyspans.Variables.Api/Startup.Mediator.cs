namespace Onlyspans.Variables.Api;

public static partial class Startup
{
    public static IServiceCollection AddMediatorServices(this IServiceCollection services)
    {
        services.AddMediator(options =>
        {
            options.ServiceLifetime = ServiceLifetime.Scoped;
        });

        return services;
    }
}
