namespace Onlyspans.Variables.Api.Startup;

public static partial class Startup
{
    private static WebApplicationBuilder AddHealthzServices(this WebApplicationBuilder builder)
    {
        builder
            .Services
            .AddHealthChecks()
            .AddNpgSql(
                connectionString: builder.Configuration.GetConnectionString("Default")!,
                name: "database",
                tags: ["ready"]);

        return builder;
    }

    private static WebApplication UseHealthz(this WebApplication app)
    {
        app.MapHealthChecks("/health");
        app.MapHealthChecks("/health/ready", new()
        {
            Predicate = check => check.Tags.Contains("ready")
        });

        return app;
    }
}
