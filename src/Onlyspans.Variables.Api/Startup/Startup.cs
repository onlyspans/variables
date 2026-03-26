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
            .AddGrpcServices()
            .AddFluentValidationServices()
            .AddHealthzServices()
            .AddMediator();

        builder.Services
            .AddOptions<GrpcClientsOptions>()
            .Bind(builder.Configuration.GetSection(GrpcClientsOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.ProjectsServiceUrl), "GrpcClients:ProjectsServiceUrl is required")
            .ValidateOnStart();

        builder.Services.AddCors(o => o.AddPolicy("open", p => p
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod()));

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

        return app;
    }
}
