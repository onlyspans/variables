using FluentValidation;

namespace Onlyspans.Variables.Api;

public static partial class Startup
{
    public static IServiceCollection AddFluentValidation(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<Program>();
        return services;
    }
}
