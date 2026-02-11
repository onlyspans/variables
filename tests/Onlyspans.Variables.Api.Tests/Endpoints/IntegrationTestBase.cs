using System.Data.Common;
using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Onlyspans.Variables.Api.Abstractions.Services;
using Onlyspans.Variables.Api.Data.Contexts;
using Testcontainers.PostgreSql;

namespace Onlyspans.Variables.Api.Tests.Endpoints;

/// <summary>
/// Base class for integration tests using WebApplicationFactory and Testcontainers
/// </summary>
public class IntegrationTestBase : IAsyncLifetime, IDisposable
{
    private PostgreSqlContainer? _postgresContainer;
    private WebApplicationFactory<Program>? _factory;
    private IServiceScope? _scope;

    protected HttpClient Client { get; private set; } = null!;
    protected ApplicationDbContext DbContext { get; private set; } = null!;

    protected Mock<IProjectsClient> MockProjectsClient { get; } = new();
    protected Mock<ITargetsPlaneClient> MockTargetsPlaneClient { get; } = new();

    public async Task InitializeAsync()
    {
        // Start PostgreSQL container
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase("variables_test")
            .WithUsername("test")
            .WithPassword("test")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
            .Build();

        await _postgresContainer.StartAsync();

        // Setup mock clients to return true by default
        MockProjectsClient
            .Setup(x => x.ProjectExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        MockTargetsPlaneClient
            .Setup(x => x.EnvironmentExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Create WebApplicationFactory
        var connectionString = _postgresContainer.GetConnectionString();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");

                // Set connection string before services are configured
                builder.UseSetting("ConnectionStrings:Default", connectionString);

                builder.ConfigureServices(services =>
                {
                    // Remove existing DbContext registration
                    var dbContextDescriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (dbContextDescriptor != null)
                    {
                        services.Remove(dbContextDescriptor);
                    }

                    var dbContextServiceDescriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(ApplicationDbContext));
                    if (dbContextServiceDescriptor != null)
                    {
                        services.Remove(dbContextServiceDescriptor);
                    }

                    // Add test database context
                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseNpgsql(connectionString);
                    });

                    // Replace gRPC clients with mocks
                    services.RemoveAll<IProjectsClient>();
                    services.RemoveAll<ITargetsPlaneClient>();
                    services.AddSingleton(MockProjectsClient.Object);
                    services.AddSingleton(MockTargetsPlaneClient.Object);
                });
            });

        Client = _factory.CreateClient();

        // Get DbContext and ensure database is created
        _scope = _factory.Services.CreateScope();
        DbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await DbContext.Database.EnsureCreatedAsync();
    }

    public void Dispose()
    {
        _scope?.Dispose();
        GC.SuppressFinalize(this);
    }

    public async Task DisposeAsync()
    {
        try
        {
            if (DbContext != null && DbContext.Database.CanConnect())
            {
                await DbContext.Database.EnsureDeletedAsync();
            }
        }
        catch
        {
            // Ignore errors during cleanup
        }

        _scope?.Dispose();

        if (DbContext != null)
        {
            await DbContext.DisposeAsync();
        }

        Client?.Dispose();

        if (_factory != null)
        {
            await _factory.DisposeAsync();
        }

        if (_postgresContainer != null)
        {
            await _postgresContainer.DisposeAsync();
        }
    }

    /// <summary>
    /// Cleans the database by removing all data from tables
    /// </summary>
    protected async Task CleanDatabaseAsync()
    {
        // Delete in order to respect foreign key constraints
        await DbContext.Database.ExecuteSqlRawAsync("DELETE FROM project_variable_set_links");
        await DbContext.Database.ExecuteSqlRawAsync("DELETE FROM variables");
        await DbContext.Database.ExecuteSqlRawAsync("DELETE FROM variable_sets");
    }

    /// <summary>
    /// Seeds the database with test data
    /// </summary>
    protected async Task SeedDatabaseAsync(Action<ApplicationDbContext> seedAction)
    {
        seedAction(DbContext);
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();
    }
}
