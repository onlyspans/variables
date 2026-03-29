namespace Onlyspans.Variables.Api.Startup;

public static partial class Startup
{
    private static WebApplicationBuilder AddCors(this WebApplicationBuilder builder)
    {
        builder
            .Services
            .AddCors(o => o
                .AddPolicy("open", p => p
                    .AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod()));

        return builder;
    }
}
