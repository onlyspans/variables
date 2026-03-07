using Onlyspans.Variables.Api.Endpoints;
using Serilog;

namespace Onlyspans.Variables.Api.Startup;

public static partial class Startup
{
    public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, configuration) =>
        {
            configuration.ReadFrom.Configuration(context.Configuration);
        });

        builder
            .AddDatabase()
            .AddGrpcServices()
            .AddFluentValidationServices()
            .AddHealthzServices()
            .AddMediator();

        return builder;
    }

    public static WebApplication Configure(this WebApplication app)
    {
        app.UseExceptionHandler();

        app.UseHealthz();
        app.UseGrpcServices();

        app.MapVariablesEndpoints();
        app.MapVariableSetsEndpoints();
        app.MapProjectVariableSetsEndpoints();

        return app;
    }
}
