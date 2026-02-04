namespace Onlyspans.Variables.Api;

public static partial class Startup
{
    public static IServiceCollection AddHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        return services;
    }
}
