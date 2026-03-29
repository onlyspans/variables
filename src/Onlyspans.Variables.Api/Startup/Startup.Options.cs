using Strongly.Options;

namespace Onlyspans.Variables.Api.Startup;

public static partial class Startup
{
    private static WebApplicationBuilder AddOptions(this WebApplicationBuilder builder)
    {
        builder
            .Services
            .AddApiStronglyOptions(builder.Configuration);

        return builder;
    }
}
