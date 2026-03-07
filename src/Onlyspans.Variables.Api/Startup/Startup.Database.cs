using Microsoft.EntityFrameworkCore;
using Onlyspans.Variables.Api.Data.Contexts;
using Onlyspans.Variables.Api.Services;

namespace Onlyspans.Variables.Api.Startup;

public static partial class Startup
{
    private static WebApplicationBuilder AddDatabase(this WebApplicationBuilder builder)
    {
        builder
            .Services
            .AddDbContextPool<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(builder.Configuration.GetConnectionString("Default"));
            });

        builder.Services.AddHostedService<MigrationHostedService>();

        return builder;
    }
}
