using Onlyspans.Variables.Api.Endpoints;
using Scalar.AspNetCore;
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
            .AddOptions()
            .AddDatabase()
            .AddGrpcServices()
            .AddFluentValidationServices()
            .AddCors()
            .AddHealthzServices()
            .AddMediator()
            .AddOpenApi();

        return builder;
    }

    public static WebApplication Configure(this WebApplication app)
    {
        app.UseExceptionHandler();

        app.UseCors("open");

        app.UseHealthz();
        app.UseGrpcServices();

        app.MapVariablesEndpoints();
        app.MapVariableSetsEndpoints();
        app.MapProjectVariableSetsEndpoints();

        app.MapScalar();

        return app;
    }
}
