using FluentValidation;

namespace Onlyspans.Variables.Api.Startup;

public static partial class Startup
{
    private static WebApplicationBuilder AddFluentValidationServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddValidatorsFromAssemblyContaining<Program>();
        return builder;
    }
}
