using Onlyspans.Variables.Api.Endpoints;
using Onlyspans.Variables.Api.Data.Options;
using Serilog;
using Strongly.Options;

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
            .AddStronglyOptions<GrpcClientsOptions>(builder.Configuration)
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
