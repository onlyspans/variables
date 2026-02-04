using Microsoft.EntityFrameworkCore;
using Onlyspans.Variables.Api.Data.Contexts;

namespace Onlyspans.Variables.Api.Services;

public sealed class MigrationHostedService(
    IServiceProvider serviceProvider,
    ILogger<MigrationHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            logger.LogInformation("Starting database migration...");
            await db.Database.MigrateAsync(cancellationToken);
            logger.LogInformation("Database migration completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during database migration: {Message}", ex.Message);
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
