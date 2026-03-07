namespace Onlyspans.Variables.Api.Startup;

public static partial class Startup
{
    private static WebApplicationBuilder AddMediator(this WebApplicationBuilder builder)
    {
        builder
            .Services
            .AddMediator(options => { options.ServiceLifetime = ServiceLifetime.Scoped; });

        return builder;
    }
}
