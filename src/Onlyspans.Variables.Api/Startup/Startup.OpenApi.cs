using Scalar.AspNetCore;

namespace Onlyspans.Variables.Api.Startup;

public static partial class Startup
{
    private static WebApplicationBuilder AddOpenApi(this WebApplicationBuilder builder)
    {
        builder
            .Services
            .AddOpenApi();

        return builder;
    }

    private static WebApplication MapScalar(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
            return app;

        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options.HideClientButton = true;
            options.Layout = ScalarLayout.Classic;
        });

        return app;
    }
}
